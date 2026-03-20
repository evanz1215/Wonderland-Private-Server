using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Wonderland.Application.ActionCodes;
using Wonderland.Application.ActionCodes.Handlers;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;
using Xunit;

namespace Wonderland.Application.Tests.ActionCodes;

public class AC9_CharacterCreationHandlerTests
{
    private readonly IPlayerRuntimeManager _players = Substitute.For<IPlayerRuntimeManager>();
    private readonly IBroadcastService _broadcast = Substitute.For<IBroadcastService>();
    private readonly ICharacterCreationService _creation = Substitute.For<ICharacterCreationService>();
    private readonly ILogger<AC9_CharacterCreationHandler> _logger = Substitute.For<ILogger<AC9_CharacterCreationHandler>>();
    private readonly AC9_CharacterCreationHandler _handler;

    public AC9_CharacterCreationHandlerTests()
    {
        _handler = new AC9_CharacterCreationHandler(_players, _broadcast, _creation, _logger);
    }

    [Fact]
    public void ActionCode_ShouldBe9()
    {
        _handler.ActionCode.Should().Be(9);
    }

    #region Sub 2 — Name validation

    [Fact]
    public async Task Sub2_ShouldSend9_3_0_WhenNameAvailable()
    {
        var session = CreateSession(new User { Id = 1 });
        var packet = CreatePacket(9, 2, writer =>
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes("TestHero"));
        });

        _creation.ValidateAndReserveNameAsync(1, "TestHero", Arg.Any<CancellationToken>())
            .Returns(NameValidationResult.Available);

        await _handler.HandleAsync(session, packet);

        await session.Received(1).SendAsync(Arg.Is<byte[]>(data =>
            data[4] == 9 && data[5] == 3 && data[6] == 0));

        // Tag should now be PendingCharacterCreation
        session.Tag.Should().BeOfType<PendingCharacterCreation>();
        var pending = (PendingCharacterCreation)session.Tag!;
        pending.ValidatedName.Should().Be("TestHero");
    }

    [Fact]
    public async Task Sub2_ShouldSend9_3_1_WhenNameTaken()
    {
        var session = CreateSession(new User { Id = 1 });
        var packet = CreatePacket(9, 2, writer =>
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes("TakenName"));
        });

        _creation.ValidateAndReserveNameAsync(1, "TakenName", Arg.Any<CancellationToken>())
            .Returns(NameValidationResult.Taken);

        await _handler.HandleAsync(session, packet);

        await session.Received(1).SendAsync(Arg.Is<byte[]>(data =>
            data[4] == 9 && data[5] == 3 && data[6] == 1));
    }

    [Fact]
    public async Task Sub2_ShouldDoNothing_WhenNoUserOnSession()
    {
        var session = CreateSession(null);
        var packet = CreatePacket(9, 2, writer =>
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes("Hero"));
        });

        await _handler.HandleAsync(session, packet);

        await session.DidNotReceive().SendAsync(Arg.Any<byte[]>());
    }

    [Fact]
    public async Task Sub2_ShouldWorkWithPendingCreationTag()
    {
        var user = new User { Id = 1 };
        var session = CreateSession(new PendingCharacterCreation(user, "OldName"));
        var packet = CreatePacket(9, 2, writer =>
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes("NewName\0"));
        });

        _creation.ValidateAndReserveNameAsync(1, "NewName", Arg.Any<CancellationToken>())
            .Returns(NameValidationResult.Available);

        await _handler.HandleAsync(session, packet);

        await session.Received(1).SendAsync(Arg.Is<byte[]>(data =>
            data[4] == 9 && data[5] == 3 && data[6] == 0));

        session.Tag.Should().BeOfType<PendingCharacterCreation>();
        ((PendingCharacterCreation)session.Tag!).ValidatedName.Should().Be("NewName");
    }

    #endregion

    #region Sub 1 — Character customization & creation

    [Fact]
    public async Task Sub1_ShouldSendError_WhenNoValidatedName()
    {
        // Tag is User (not PendingCharacterCreation) — name not validated yet
        var session = CreateSession(new User { Id = 1 });
        var packet = CreateSub1Packet();

        await _handler.HandleAsync(session, packet);

        await session.Received(1).SendAsync(Arg.Is<byte[]>(data =>
            data[4] == 0 && data[5] == 30));
    }

    [Fact]
    public async Task Sub1_ShouldCreateCharacter_WhenAllValid()
    {
        var user = new User { Id = 1, CipherPassword = "existing" };
        var session = CreateSession(new PendingCharacterCreation(user, "Hero"));
        var packet = CreateSub1Packet();

        _creation.GetNextAvailableSlotAsync(1, Arg.Any<CancellationToken>()).Returns((byte?)0);

        var character = new Character
        {
            CharId = 42, UserId = 1, Name = "Hero", Level = 1, Body = 1, Head = 2,
            Hair = 3, Skin = 4, Clothing = 5, Eyes = 6, Affinity = Affinity.Fire,
            MapId = 60000, X = 602, Y = 455, MaxHp = 98, CurrentHp = 98, MaxSp = 48, CurrentSp = 48,
        };

        _creation.CreateCharacterAsync(Arg.Any<CharacterCreationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CharacterCreationResult(CharacterCreationStatus.Success, character));

        await _handler.HandleAsync(session, packet);

        // Should register player
        _players.Received(1).Add(Arg.Is<PlayerRuntime>(p => p.CharName == "Hero"));

        // Should send AC 3 (appearance) and AC 5,3 (stats) — at least 2 sends
        await session.Received(2).SendAsync(Arg.Any<byte[]>());
    }

    [Fact]
    public async Task Sub1_ShouldSetCipher_WhenNotYetSet()
    {
        var user = new User { Id = 1, CipherPassword = "" };
        var session = CreateSession(new PendingCharacterCreation(user, "Hero"));
        var packet = CreateSub1PacketWithCipher("secret");

        _creation.SetCipherPasswordAsync(1, "secret", Arg.Any<CancellationToken>()).Returns(true);
        _creation.GetNextAvailableSlotAsync(1, Arg.Any<CancellationToken>()).Returns((byte?)0);

        var character = new Character
        {
            CharId = 42, UserId = 1, Name = "Hero", Level = 1,
            MapId = 60000, X = 602, Y = 455, MaxHp = 50, CurrentHp = 50,
        };
        _creation.CreateCharacterAsync(Arg.Any<CharacterCreationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CharacterCreationResult(CharacterCreationStatus.Success, character));

        await _handler.HandleAsync(session, packet);

        await _creation.Received(1).SetCipherPasswordAsync(1, "secret", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sub1_ShouldSendError_WhenCipherInvalid()
    {
        var user = new User { Id = 1, CipherPassword = "" };
        var session = CreateSession(new PendingCharacterCreation(user, "Hero"));
        // Cipher too short: "abc" (3 chars < 6)
        var packet = CreateSub1PacketWithCipher("abc");

        await _handler.HandleAsync(session, packet);

        await session.Received(1).SendAsync(Arg.Is<byte[]>(data =>
            data[4] == 0 && data[5] == 30));
    }

    [Fact]
    public async Task Sub1_ShouldSendError_WhenNoSlotAvailable()
    {
        var user = new User { Id = 1, CipherPassword = "existing" };
        var session = CreateSession(new PendingCharacterCreation(user, "Hero"));
        var packet = CreateSub1Packet();

        _creation.GetNextAvailableSlotAsync(1, Arg.Any<CancellationToken>()).Returns((byte?)null);

        await _handler.HandleAsync(session, packet);

        await session.Received(1).SendAsync(Arg.Is<byte[]>(data =>
            data[4] == 0 && data[5] == 30));
    }

    [Fact]
    public async Task Sub1_ShouldSendError_WhenCreationFails()
    {
        var user = new User { Id = 1, CipherPassword = "existing" };
        var session = CreateSession(new PendingCharacterCreation(user, "Hero"));
        var packet = CreateSub1Packet();

        _creation.GetNextAvailableSlotAsync(1, Arg.Any<CancellationToken>()).Returns((byte?)0);
        _creation.CreateCharacterAsync(Arg.Any<CharacterCreationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CharacterCreationResult(CharacterCreationStatus.Failed, ErrorMessage: "DB error"));

        await _handler.HandleAsync(session, packet);

        await session.Received(1).SendAsync(Arg.Is<byte[]>(data =>
            data[4] == 0 && data[5] == 30));
        _players.DidNotReceive().Add(Arg.Any<PlayerRuntime>());
    }

    #endregion

    #region Helpers

    private static IGameSession CreateSession(object? tag)
    {
        var session = Substitute.For<IGameSession>();
        session.SessionId.Returns("test-session");
        session.Tag = tag;
        // NSubstitute needs proper get/set for Tag
        var currentTag = tag;
        session.Tag = Arg.Do<object?>(v => currentTag = v);
        session.Tag.Returns(_ => currentTag);
        return session;
    }

    private static IReceivePacket CreatePacket(byte ac, byte sub, Action<BinaryWriter> writePayload)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writePayload(writer);
        var payload = ms.ToArray();

        var packet = Substitute.For<IReceivePacket>();
        packet.ActionCode.Returns(ac);
        packet.SubAction.Returns(sub);
        packet.Remaining.Returns(payload.Length);

        var offset = 0;
        packet.ReadString(Arg.Any<int>()).Returns(callInfo =>
        {
            var len = callInfo.ArgAt<int>(0);
            var actualLen = Math.Min(len, payload.Length - offset);
            var result = System.Text.Encoding.UTF8.GetString(payload, offset, actualLen);
            offset += actualLen;
            return result;
        });

        return packet;
    }

    /// <summary>
    /// Creates a Sub1 packet with appearance data but NO cipher (user already has one).
    /// [body:16][head:16][hair:16][skin:16][clothing:16][eyes:16][element:8][str:8][agi:8][wis:8][int:8][con:8]
    /// </summary>
    private static IReceivePacket CreateSub1Packet()
    {
        var packet = Substitute.For<IReceivePacket>();
        packet.ActionCode.Returns((byte)9);
        packet.SubAction.Returns((byte)1);

        var readU16Queue = new Queue<ushort>([1, 2, 3, 4, 5, 6]); // body, head, hair, skin, clothing, eyes
        packet.ReadUInt16().Returns(_ => readU16Queue.Dequeue());

        var readU8Queue = new Queue<byte>([(byte)Affinity.Fire, 10, 8, 6, 4, 12]); // element, str, agi, wis, int, con
        packet.ReadByte().Returns(_ => readU8Queue.Dequeue());

        packet.Remaining.Returns(0); // No cipher data

        return packet;
    }

    /// <summary>
    /// Creates a Sub1 packet WITH cipher password data.
    /// </summary>
    private static IReceivePacket CreateSub1PacketWithCipher(string cipher)
    {
        var packet = Substitute.For<IReceivePacket>();
        packet.ActionCode.Returns((byte)9);
        packet.SubAction.Returns((byte)1);

        var readU16Queue = new Queue<ushort>([1, 2, 3, 4, 5, 6]);
        packet.ReadUInt16().Returns(_ => readU16Queue.Dequeue());

        var readU8Queue = new Queue<byte>([(byte)Affinity.Fire, 10, 8, 6, 4, 12]);
        packet.ReadByte().Returns(_ => readU8Queue.Dequeue());

        packet.Remaining.Returns(cipher.Length + 1); // cipher + null terminator
        packet.ReadString(Arg.Any<int>()).Returns(cipher);

        return packet;
    }

    #endregion
}
