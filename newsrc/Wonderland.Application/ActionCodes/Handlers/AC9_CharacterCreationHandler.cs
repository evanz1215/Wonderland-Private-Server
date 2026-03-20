using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;

namespace Wonderland.Application.ActionCodes.Handlers;

/// <summary>
/// AC9 — Character creation.
/// Sub 1: Set appearance, stats, and cipher password → create character
/// Sub 2: Validate character name
/// </summary>
public class AC9_CharacterCreationHandler : ActionCodeHandlerBase
{
    private readonly ICharacterCreationService _creation;

    public override byte ActionCode => 9;
    protected override bool RequiresPlayer => false; // Pre-login: player not yet created

    public AC9_CharacterCreationHandler(
        IPlayerRuntimeManager players,
        IBroadcastService broadcast,
        ICharacterCreationService creation,
        ILogger<AC9_CharacterCreationHandler> logger)
        : base(players, broadcast, logger)
    {
        _creation = creation;
    }

    protected override async Task HandleCoreAsync(
        IGameSession session, IReceivePacket packet, PlayerRuntime? player, CancellationToken ct)
    {
        switch (packet.SubAction)
        {
            case 1:
                await HandleCustomizationAsync(session, packet, ct);
                break;
            case 2:
                await HandleNameValidationAsync(session, packet, ct);
                break;
            default:
                Logger.LogDebug("AC9 unhandled sub {Sub}", packet.SubAction);
                break;
        }
    }

    /// <summary>
    /// Sub 1: Character customization + cipher password + creation.
    /// Packet: [body:16][head:16][hairColor:16][skinColor:16][clothingColor:16][eyeColor:16]
    ///         [element:8][str:8][agi:8][wis:8][int:8][con:8][cipherPassword:stringN]
    /// </summary>
    private async Task HandleCustomizationAsync(IGameSession session, IReceivePacket packet, CancellationToken ct)
    {
        // Extract user and validated name from session tag
        User user;
        string charName;

        switch (session.Tag)
        {
            case PendingCharacterCreation pending:
                user = pending.User;
                charName = pending.ValidatedName;
                break;
            case User u:
                // Name should have been validated via Sub2 first
                Logger.LogWarning("AC9 sub1: no validated name on session {Session}", session.SessionId);
                await SendAsync(session, PacketFactory.Create(0, 30).Build());
                return;
            default:
                Logger.LogWarning("AC9 sub1: no user on session {Session}", session.SessionId);
                return;
        }

        // Parse appearance
        var body = packet.ReadUInt16();
        var head = packet.ReadUInt16();
        var hairColor = packet.ReadUInt16();
        var skinColor = packet.ReadUInt16();
        var clothingColor = packet.ReadUInt16();
        var eyeColor = packet.ReadUInt16();

        // Parse element and stats
        var element = (Affinity)packet.ReadByte();
        var str = packet.ReadByte();
        var agi = packet.ReadByte();
        var wis = packet.ReadByte();
        var intStat = packet.ReadByte();
        var con = packet.ReadByte();

        // Handle cipher password (only if not yet set)
        if (string.IsNullOrEmpty(user.CipherPassword))
        {
            var cipher = packet.ReadString(packet.Remaining - 1); // Read remaining as null-terminated string
            if (cipher.Length is < 6 or > 14)
            {
                Logger.LogWarning("AC9 sub1: invalid cipher length {Len} for user {UserId}", cipher.Length, user.Id);
                await SendAsync(session, PacketFactory.Create(0, 30).Build());
                return;
            }

            if (!await _creation.SetCipherPasswordAsync(user.Id, cipher, ct))
            {
                await SendAsync(session, PacketFactory.Create(0, 30).Build());
                return;
            }

            user.CipherPassword = cipher;
        }

        // Determine slot
        var slot = await _creation.GetNextAvailableSlotAsync(user.Id, ct);
        if (slot is null)
        {
            Logger.LogWarning("AC9 sub1: no available slot for user {UserId}", user.Id);
            await SendAsync(session, PacketFactory.Create(0, 30).Build());
            return;
        }

        // Create character
        var request = new CharacterCreationRequest(
            UserId: user.Id,
            Slot: slot.Value,
            Name: charName,
            Body: body,
            Head: head,
            HairColor: hairColor,
            SkinColor: skinColor,
            ClothingColor: clothingColor,
            EyeColor: eyeColor,
            Element: element,
            Str: str,
            Agi: agi,
            Wis: wis,
            Int: intStat,
            Con: con
        );

        var result = await _creation.CreateCharacterAsync(request, ct);

        if (result.Status != CharacterCreationStatus.Success || result.Character is null)
        {
            Logger.LogWarning("AC9 sub1: creation failed — {Status} for user {UserId}", result.Status, user.Id);
            await SendAsync(session, PacketFactory.Create(0, 30).Build());
            return;
        }

        // Create runtime and register
        var playerRuntime = PlayerRuntime.FromCharacter(result.Character, user, session.SessionId);
        Players.Add(playerRuntime);
        session.Tag = playerRuntime;

        // Send login sequence (same as AC63 character select)
        await SendCharacterLoginDataAsync(session, playerRuntime);

        Logger.LogInformation("Character created: {Name} (CharID: {CharId}) for user {UserId}",
            playerRuntime.CharName, playerRuntime.CharId, user.Id);
    }

    /// <summary>
    /// Sub 2: Name validation.
    /// Packet: [name:stringN]
    /// Response: [9][3][0] = OK, [9][3][1] = taken/invalid
    /// </summary>
    private async Task HandleNameValidationAsync(IGameSession session, IReceivePacket packet, CancellationToken ct)
    {
        User user = session.Tag switch
        {
            User u => u,
            PendingCharacterCreation p => p.User,
            _ => null!,
        };

        if (user is null)
        {
            Logger.LogWarning("AC9 sub2: no user on session {Session}", session.SessionId);
            return;
        }

        var name = packet.ReadString(packet.Remaining);
        // Trim null terminator if present
        name = name.TrimEnd('\0');

        var validationResult = await _creation.ValidateAndReserveNameAsync(user.Id, name, ct);

        if (validationResult == NameValidationResult.Available)
        {
            // Store the validated name temporarily on session for Sub1 to use
            // We use a tuple to keep both User and the pending name
            session.Tag = new PendingCharacterCreation(user, name);
            await SendAsync(session, PacketFactory.Create(9, 3).U8(0).Build());
            Logger.LogDebug("Name validated: {Name} for user {UserId}", name, user.Id);
        }
        else
        {
            await SendAsync(session, PacketFactory.Create(9, 3).U8(1).Build());
            Logger.LogDebug("Name rejected: {Name} — {Reason}", name, validationResult);
        }
    }

    private static async Task SendCharacterLoginDataAsync(IGameSession session, PlayerRuntime p)
    {
        // AC 3: Character appearance
        var charData = PacketFactory.Create(3)
            .U32((uint)p.CharId)
            .U8((byte)p.Body)
            .U16(p.MapId)
            .U16(p.X)
            .U16(p.Y)
            .U8(0)
            .U16(p.Head)
            .U16(p.Hair)
            .U16(p.Skin)
            .U16(p.Clothing)
            .U16(p.Eyes)
            .U8(0) // equipment count
            .Pad(4)
            .NStr(p.CharName)
            .NStr("") // nickname
            .U8(0)
            .Build();
        await session.SendAsync(charData);

        // AC 5,3: Stats and info
        var loginInfo = PacketFactory.Create(5, 3)
            .U8((byte)p.Affinity)
            .I32(p.CurrentHp)
            .U16((ushort)p.CurrentSp)
            .U16(p.Str)
            .U16(p.Con)
            .U16(p.Agi)
            .U16(p.Int)
            .U16(p.Wis)
            .U8(p.Level)
            .I64(p.TotalExp)
            .U16(p.PotentialPoints)
            .U16(p.SkillPoints)
            .I32(p.Gold)
            .I32(p.MaxHp)
            .U16((ushort)p.MaxSp)
            .U8((byte)p.RebornJob)
            .Build();
        await session.SendAsync(loginInfo);
    }
}

/// <summary>
/// Holds user + validated name between AC9 Sub2 (name check) and Sub1 (creation).
/// </summary>
public record PendingCharacterCreation(User User, string ValidatedName);
