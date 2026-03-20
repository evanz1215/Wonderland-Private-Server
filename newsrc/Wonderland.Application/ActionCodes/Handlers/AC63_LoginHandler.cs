using System.Text;
using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;

namespace Wonderland.Application.ActionCodes.Handlers;

/// <summary>
/// AC63 — Character select and account login.
/// Sub 4: Account login with password verification
/// Sub 2: Character slot selection
/// </summary>
public class AC63_LoginHandler : ActionCodeHandlerBase
{
    private const ushort MinClientVersion = 1096;

    private readonly IAuthService _auth;

    public override byte ActionCode => 63;
    protected override bool RequiresPlayer => false; // Pre-login handler

    public AC63_LoginHandler(
        IPlayerRuntimeManager players,
        IBroadcastService broadcast,
        IAuthService auth,
        ILogger<AC63_LoginHandler> logger)
        : base(players, broadcast, logger)
    {
        _auth = auth;
    }

    protected override async Task HandleCoreAsync(
        IGameSession session, IReceivePacket packet, PlayerRuntime? player, CancellationToken ct)
    {
        switch (packet.SubAction)
        {
            case 4:
                await HandleLoginAsync(session, packet, ct);
                break;
            case 2:
                await HandleCharacterSelectAsync(session, packet, player, ct);
                break;
            default:
                Logger.LogDebug("AC63 unhandled sub {Sub}", packet.SubAction);
                break;
        }
    }

    private async Task HandleLoginAsync(IGameSession session, IReceivePacket packet, CancellationToken ct)
    {
        // Parse: [version:16][nameLen:8][name:bytes][passLen:8][pass:bytes]
        var version = packet.ReadUInt16();
        var nameLen = packet.ReadByte();
        var username = packet.ReadString(nameLen);
        var passLen = packet.ReadByte();
        var password = packet.ReadString(passLen);

        Logger.LogInformation("Login attempt: {Username} (version {Version})", username, version);

        // Version check
        if (version < MinClientVersion)
        {
            await SendAsync(session, PacketFactory.Create(0, 17).Build());
            return;
        }

        // Authenticate
        var result = await _auth.LoginAsync(username, password, version, ct);

        switch (result.Status)
        {
            case AuthStatus.Success:
                await OnLoginSuccessAsync(session, result.User!, ct);
                break;

            case AuthStatus.AlreadyOnline:
                await SendAsync(session, PacketFactory.Create(63, 2).Build());
                await SendAsync(session, PacketFactory.Create(0, 19).Build());
                break;

            case AuthStatus.InvalidCredentials:
            case AuthStatus.Banned:
            default:
                await SendAsync(session, PacketFactory.Create(63, 2).Build());
                await SendAsync(session, PacketFactory.Create(1, 6).Build());
                break;
        }
    }

    private async Task OnLoginSuccessAsync(IGameSession session, User user, CancellationToken ct)
    {
        // Store user ID on the session for character select
        session.Tag = user;

        // Send: [63][2][userID:32]
        await SendAsync(session, PacketFactory.Create(63, 2).U32((uint)user.Id).Build());

        // Send character list
        var characters = await _auth.GetCharacterListAsync(user.Id, ct);
        foreach (var c in characters)
        {
            // Send character preview data for each slot
            var charPacket = BuildCharacterPreview(c);
            await SendAsync(session, charPacket);
        }

        // Send login complete marker
        await SendAsync(session, PacketFactory.Create(35, 11).Build());

        Logger.LogInformation("Login success: {Username} (UserID: {UserId})", user.Username, user.Id);
    }

    private async Task HandleCharacterSelectAsync(
        IGameSession session, IReceivePacket packet, PlayerRuntime? player, CancellationToken ct)
    {
        if (session.Tag is not User user)
        {
            Logger.LogWarning("AC63 sub2: no user on session {Session}", session.SessionId);
            return;
        }

        var slot = packet.ReadByte();
        var character = await _auth.SelectCharacterAsync(user.Id, slot, ct);

        if (character is null)
        {
            // No character in this slot — signal character creation
            // [1][3][hasCipher:8]
            byte hasCipher = string.IsNullOrEmpty(user.CipherPassword) ? (byte)0 : (byte)1;
            await SendAsync(session, PacketFactory.Create(1, 3).U8(hasCipher).Build());
            return;
        }

        // Create runtime player and register
        var runtime = PlayerRuntime.FromCharacter(character, user, session.SessionId);
        Players.Add(runtime);
        session.Tag = runtime;

        // Send character full data
        await SendCharacterLoginDataAsync(session, runtime);

        Logger.LogInformation("Character selected: {Name} (CharID: {CharId})", runtime.CharName, runtime.CharId);
    }

    private async Task SendCharacterLoginDataAsync(IGameSession session, PlayerRuntime p)
    {
        // AC 3: Character appearance data for other players
        var charData = PacketFactory.Create(3)
            .U32((uint)p.CharId)
            .U8((byte)p.Body)
            .U16(p.MapId)
            .U16(p.X)
            .U16(p.Y)
            .U8(0) // padding
            .U16(p.Head)
            .U16(p.Hair)
            .U16(p.Skin)
            .U16(p.Clothing)
            .U16(p.Eyes)
            .U8(0) // worn equipment count (TODO: load from inventory)
            .Pad(4)
            .NStr(p.CharName)
            .NStr("") // nickname
            .U8(0)
            .Build();
        await SendAsync(session, charData);

        // AC 5,3: Login player info (element, HP/SP, stats, level, exp)
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
        await SendAsync(session, loginInfo);
    }

    private static byte[] BuildCharacterPreview(Character c)
    {
        return PacketFactory.Create(63, 3)
            .U8(c.Slot)
            .U32((uint)c.CharId)
            .NStr(c.Name)
            .U8(c.Level)
            .U8((byte)c.RebornJob)
            .U16(c.Body)
            .U16(c.Head)
            .U16(c.Hair)
            .Build();
    }
}
