using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Wonderland.Application.ActionCodes;
using Wonderland.Application.ActionCodes.Handlers;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;
using Xunit;

namespace Wonderland.Application.Tests.ActionCodes;

public class AC23_InventoryHandlerTests
{
    private readonly IPlayerRuntimeManager _players = Substitute.For<IPlayerRuntimeManager>();
    private readonly IBroadcastService _broadcast = Substitute.For<IBroadcastService>();
    private readonly IInventoryService _inventory = Substitute.For<IInventoryService>();
    private readonly ILogger<AC23_InventoryHandler> _logger = Substitute.For<ILogger<AC23_InventoryHandler>>();
    private readonly AC23_InventoryHandler _handler;

    public AC23_InventoryHandlerTests()
    {
        _handler = new AC23_InventoryHandler(_players, _broadcast, _inventory, _logger);
    }

    [Fact]
    public void ActionCode_ShouldBe23()
    {
        _handler.ActionCode.Should().Be(23);
    }

    #region Sub 10 — Move

    [Fact]
    public async Task Sub10_ShouldCallMoveItem()
    {
        var (session, player) = CreateSessionWithPlayer();
        var packet = CreatePacket(23, 10, [5, 3, 10]); // from=5, qty=3, to=10

        _inventory.MoveItemAsync(42, 5, 10, 3, Arg.Any<CancellationToken>())
            .Returns(new InventoryResult(InventoryStatus.Success, new InventoryItem()));

        await _handler.HandleAsync(session, packet);

        await _inventory.Received(1).MoveItemAsync(42, 5, 10, 3, Arg.Any<CancellationToken>());
        await session.Received(1).SendAsync(Arg.Is<byte[]>(d => d[4] == 23 && d[5] == 10));
    }

    [Fact]
    public async Task Sub10_ShouldNotSend_WhenMoveFails()
    {
        var (session, player) = CreateSessionWithPlayer();
        var packet = CreatePacket(23, 10, [5, 3, 10]);

        _inventory.MoveItemAsync(42, 5, 10, 3, Arg.Any<CancellationToken>())
            .Returns(new InventoryResult(InventoryStatus.SlotEmpty));

        await _handler.HandleAsync(session, packet);

        await session.DidNotReceive().SendAsync(Arg.Any<byte[]>());
    }

    [Fact]
    public async Task Sub10_ShouldIgnore_WhenSameSlot()
    {
        var (session, player) = CreateSessionWithPlayer();
        var packet = CreatePacket(23, 10, [5, 1, 5]); // from=5, to=5

        await _handler.HandleAsync(session, packet);

        await _inventory.DidNotReceive().MoveItemAsync(Arg.Any<int>(), Arg.Any<byte>(), Arg.Any<byte>(),
            Arg.Any<ushort>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sub10_ShouldIgnore_WhenSlotOutOfRange()
    {
        var (session, player) = CreateSessionWithPlayer();
        var packet = CreatePacket(23, 10, [0, 1, 5]); // from=0 invalid

        await _handler.HandleAsync(session, packet);

        await _inventory.DidNotReceive().MoveItemAsync(Arg.Any<int>(), Arg.Any<byte>(), Arg.Any<byte>(),
            Arg.Any<ushort>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Sub 11 — Equip

    [Fact]
    public async Task Sub11_ShouldSendEquipConfirmAndBroadcast()
    {
        var (session, player) = CreateSessionWithPlayer();
        var packet = CreatePacket(23, 11, [3]); // invSlot=3

        var item = new InventoryItem { ItemId = 500 };
        _inventory.EquipItemAsync(42, 3, Arg.Any<CancellationToken>())
            .Returns(new EquipResult(InventoryStatus.Success, item, 2, 3));

        await _handler.HandleAsync(session, packet);

        // AC 23,17 confirm
        await session.Received(1).SendAsync(Arg.Is<byte[]>(d => d[4] == 23 && d[5] == 17));
        // AC 5,2 broadcast
        await _broadcast.Received(1).BroadcastToMapAsync(1000, Arg.Is<byte[]>(d => d[4] == 5 && d[5] == 2),
            Arg.Any<int?>());
    }

    [Fact]
    public async Task Sub11_ShouldNotSend_WhenEquipFails()
    {
        var (session, player) = CreateSessionWithPlayer();
        var packet = CreatePacket(23, 11, [3]);

        _inventory.EquipItemAsync(42, 3, Arg.Any<CancellationToken>())
            .Returns(new EquipResult(InventoryStatus.SlotEmpty));

        await _handler.HandleAsync(session, packet);

        await session.DidNotReceive().SendAsync(Arg.Any<byte[]>());
    }

    #endregion

    #region Sub 12 — Unequip

    [Fact]
    public async Task Sub12_ShouldSendUnequipConfirmAndBroadcast()
    {
        var (session, player) = CreateSessionWithPlayer();
        var packet = CreatePacket(23, 12, [2, 5]); // equipSlot=2, invSlot=5

        _inventory.UnequipItemAsync(42, 2, 5, Arg.Any<CancellationToken>())
            .Returns(new EquipResult(InventoryStatus.Success, new InventoryItem(), 2, 5));

        await _handler.HandleAsync(session, packet);

        // AC 23,16 confirm
        await session.Received(1).SendAsync(Arg.Is<byte[]>(d => d[4] == 23 && d[5] == 16));
        // AC 5,1 broadcast
        await _broadcast.Received(1).BroadcastToMapAsync(1000, Arg.Is<byte[]>(d => d[4] == 5 && d[5] == 1),
            Arg.Any<int?>());
    }

    [Fact]
    public async Task Sub12_ShouldNotSend_WhenUnequipFails()
    {
        var (session, player) = CreateSessionWithPlayer();
        var packet = CreatePacket(23, 12, [2, 5]);

        _inventory.UnequipItemAsync(42, 2, 5, Arg.Any<CancellationToken>())
            .Returns(new EquipResult(InventoryStatus.SlotOccupied));

        await _handler.HandleAsync(session, packet);

        await session.DidNotReceive().SendAsync(Arg.Any<byte[]>());
    }

    [Fact]
    public async Task Sub12_ShouldIgnore_WhenEquipSlotOutOfRange()
    {
        var (session, player) = CreateSessionWithPlayer();
        var packet = CreatePacket(23, 12, [7, 5]); // equipSlot=7 invalid

        await _handler.HandleAsync(session, packet);

        await _inventory.DidNotReceive().UnequipItemAsync(Arg.Any<int>(), Arg.Any<byte>(), Arg.Any<byte>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Sub 3 — Drop

    [Fact]
    public async Task Sub3_ShouldCallDestroy_AndSendRemovePacket()
    {
        var (session, player) = CreateSessionWithPlayer();
        var packet = CreatePacket(23, 3, [5, 2, 0]); // slot=5, qty=2, unk=0

        var item = new InventoryItem { ItemId = 100 };
        _inventory.DestroyItemAsync(42, 5, 2, Arg.Any<CancellationToken>())
            .Returns(new InventoryResult(InventoryStatus.Success, item));

        await _handler.HandleAsync(session, packet);

        await session.Received(1).SendAsync(Arg.Is<byte[]>(d => d[4] == 23 && d[5] == 9));
    }

    #endregion

    #region Sub 124 — Confirm Destroy

    [Fact]
    public async Task Sub124_ShouldCallDestroy_AndSendDestroyConfirm()
    {
        var (session, player) = CreateSessionWithPlayer();
        var packet = CreatePacket(23, 124, [5, 1, 0]); // slot=5, qty=1, unk=0

        var item = new InventoryItem { ItemId = 300 };
        _inventory.DestroyItemAsync(42, 5, 1, Arg.Any<CancellationToken>())
            .Returns(new InventoryResult(InventoryStatus.Success, item));

        await _handler.HandleAsync(session, packet);

        // AC 23,26 destroy confirmed
        await session.Received(1).SendAsync(Arg.Is<byte[]>(d => d[4] == 23 && d[5] == 26));
    }

    #endregion

    #region Static packet builders

    [Fact]
    public void BuildInventoryStatePacket_ShouldIncludeAllItems()
    {
        var items = new List<InventoryItem>
        {
            new() { Slot = 1, ItemId = 100, Quantity = 5, Forge = Domain.Enums.ForgeLevel.Plus3 },
            new() { Slot = 5, ItemId = 200, Quantity = 1 },
        };

        var packet = AC23_InventoryHandler.BuildInventoryStatePacket(items);
        packet[4].Should().Be(23);
        packet[5].Should().Be(5);
        // First item starts at offset 6
        packet[6].Should().Be(1); // slot
    }

    [Fact]
    public void BuildInventoryStatePacket_ShouldSkipZeroItemId()
    {
        var items = new List<InventoryItem>
        {
            new() { Slot = 1, ItemId = 0, Quantity = 0 },
            new() { Slot = 2, ItemId = 100, Quantity = 1 },
        };

        var packet = AC23_InventoryHandler.BuildInventoryStatePacket(items);
        // Only 1 item should be written (slot 2)
        packet[6].Should().Be(2); // First written slot should be 2
    }

    [Fact]
    public void BuildEquipmentStatePacket_ShouldIncludeEquipped()
    {
        var items = new List<InventoryItem>
        {
            new() { EquipSlot = 1, ItemId = 500 },
        };

        var packet = AC23_InventoryHandler.BuildEquipmentStatePacket(items);
        packet[4].Should().Be(23);
        packet[5].Should().Be(11);
        packet[6].Should().Be(1); // equipSlot
    }

    #endregion

    #region Edge cases

    [Fact]
    public async Task ShouldDoNothing_WhenPlayerNull()
    {
        var session = Substitute.For<IGameSession>();
        session.SessionId.Returns("test");
        _players.GetBySessionId("test").Returns((PlayerRuntime?)null);

        var packet = CreatePacket(23, 10, [1, 1, 2]);

        await _handler.HandleAsync(session, packet);

        await _inventory.DidNotReceive().MoveItemAsync(Arg.Any<int>(), Arg.Any<byte>(), Arg.Any<byte>(),
            Arg.Any<ushort>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private (IGameSession session, PlayerRuntime player) CreateSessionWithPlayer()
    {
        var session = Substitute.For<IGameSession>();
        session.SessionId.Returns("test-session");

        var player = new PlayerRuntime
        {
            UserId = 1,
            CharId = 42,
            SessionId = "test-session",
            CharName = "TestPlayer",
            MapId = 1000,
        };

        _players.GetBySessionId("test-session").Returns(player);
        return (session, player);
    }

    private static IReceivePacket CreatePacket(byte ac, byte sub, byte[] payload)
    {
        var packet = Substitute.For<IReceivePacket>();
        packet.ActionCode.Returns(ac);
        packet.SubAction.Returns(sub);

        var queue = new Queue<byte>(payload);
        packet.ReadByte().Returns(_ => queue.Count > 0 ? queue.Dequeue() : (byte)0);

        return packet;
    }

    #endregion
}
