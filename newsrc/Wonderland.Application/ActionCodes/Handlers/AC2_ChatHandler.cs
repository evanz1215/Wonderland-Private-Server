using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;

namespace Wonderland.Application.ActionCodes.Handlers;

/// <summary>
/// AC2 — Chat system.
/// Sub 1: Whisper
/// Sub 2: Local/map chat (+ GM commands)
/// Sub 3: Team chat
/// Sub 5: World chat
/// </summary>
public class AC2_ChatHandler : ActionCodeHandlerBase
{
    private readonly IGmCommandService _gmCommands;
    private const char GmCommandPrefix = ':';

    public override byte ActionCode => 2;

    public AC2_ChatHandler(
        IPlayerRuntimeManager players,
        IBroadcastService broadcast,
        IGmCommandService gmCommands,
        ILogger<AC2_ChatHandler> logger)
        : base(players, broadcast, logger)
    {
        _gmCommands = gmCommands;
    }

    protected override async Task HandleCoreAsync(
        IGameSession session, IReceivePacket packet, PlayerRuntime? player, CancellationToken ct)
    {
        if (player is null) return;

        switch (packet.SubAction)
        {
            case 1: await HandleWhisperAsync(session, packet, player, ct); break;
            case 2: await HandleLocalChatAsync(session, packet, player, ct); break;
            case 3: await HandleTeamChatAsync(session, packet, player, ct); break;
            case 5: await HandleWorldChatAsync(session, packet, player, ct); break;
            default:
                Logger.LogDebug("AC2 unhandled sub {Sub}", packet.SubAction);
                break;
        }
    }

    private async Task HandleWhisperAsync(
        IGameSession session, IReceivePacket packet, PlayerRuntime player, CancellationToken ct)
    {
        if (player.IsMuted) return;

        var targetName = ReadNullTerminatedString(packet);
        var message = ReadNullTerminatedString(packet);
        if (string.IsNullOrEmpty(targetName) || string.IsNullOrEmpty(message)) return;

        var target = Players.GetByCharName(targetName);
        if (target is null)
        {
            // Player not found — send error
            await SendSystemMessage(session, $"Player '{targetName}' is not online.");
            return;
        }

        // Send whisper to target: [2][1][senderCharID:32][senderName:str\0][message:str\0]
        var whisperPacket = PacketFactory.Create(2, 1)
            .U32((uint)player.CharId)
            .NStr(player.CharName)
            .NStr(message)
            .Build();

        await Broadcast.SendToPlayerAsync(target.CharId, whisperPacket);
    }

    private async Task HandleLocalChatAsync(
        IGameSession session, IReceivePacket packet, PlayerRuntime player, CancellationToken ct)
    {
        var message = ReadRemainingString(packet);
        if (string.IsNullOrEmpty(message)) return;

        // GM command check
        if (message.StartsWith(GmCommandPrefix) && player.GmLevel >= GmStatus.GameMaster)
        {
            var result = await _gmCommands.ExecuteAsync(player, message, ct);
            await SendSystemMessage(session, result.Message);
            return;
        }

        if (player.IsMuted) return;

        // Broadcast to map: [2][2][charID:32][message:str\0]
        var chatPacket = PacketFactory.Create(2, 2)
            .U32((uint)player.CharId)
            .NStr(message)
            .Build();

        await BroadcastToMapAsync(player, chatPacket);
    }

    private async Task HandleTeamChatAsync(
        IGameSession session, IReceivePacket packet, PlayerRuntime player, CancellationToken ct)
    {
        if (player.IsMuted || !player.TeamId.HasValue) return;

        var message = ReadRemainingString(packet);
        if (string.IsNullOrEmpty(message)) return;

        var teamPacket = PacketFactory.Create(2, 3)
            .U32((uint)player.CharId)
            .NStr(message)
            .Build();

        await Broadcast.BroadcastToTeamAsync(player.TeamId.Value, teamPacket);
    }

    private async Task HandleWorldChatAsync(
        IGameSession session, IReceivePacket packet, PlayerRuntime player, CancellationToken ct)
    {
        if (player.IsMuted) return;

        var message = ReadRemainingString(packet);
        if (string.IsNullOrEmpty(message)) return;

        var worldPacket = PacketFactory.Create(2, 5)
            .U32((uint)player.CharId)
            .NStr(message)
            .Build();

        await Broadcast.BroadcastToAllAsync(worldPacket);
    }

    // --- Helpers ---

    private Task SendSystemMessage(IGameSession session, string message)
    {
        var packet = PacketFactory.Create(2, 6).NStr(message).Build();
        return SendAsync(session, packet);
    }

    private static string ReadNullTerminatedString(IReceivePacket packet)
    {
        var bytes = new List<byte>();
        while (packet.Remaining > 0)
        {
            var b = packet.ReadByte();
            if (b == 0) break;
            bytes.Add(b);
        }
        return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static string ReadRemainingString(IReceivePacket packet)
    {
        if (packet.Remaining <= 0) return string.Empty;
        var bytes = packet.ReadBytes(packet.Remaining);
        return System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
    }
}
