using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;

namespace Wonderland.Application.ActionCodes;

/// <summary>
/// Abstract base class for all AC handlers.
/// Provides common infrastructure: player lookup, logging, packet building, broadcast.
/// Subclasses only need to implement HandleCoreAsync().
/// </summary>
public abstract class ActionCodeHandlerBase : IActionCodeHandler
{
    protected readonly IPlayerRuntimeManager Players;
    protected readonly IBroadcastService Broadcast;
    protected readonly ILogger Logger;

    public abstract byte ActionCode { get; }

    protected ActionCodeHandlerBase(
        IPlayerRuntimeManager players,
        IBroadcastService broadcast,
        ILogger logger)
    {
        Players = players;
        Broadcast = broadcast;
        Logger = logger;
    }

    public async Task HandleAsync(IGameSession session, IReceivePacket packet, CancellationToken ct = default)
    {
        var player = Players.GetBySessionId(session.SessionId);

        // Some ACs (like AC63 login) work before a player is assigned
        if (RequiresPlayer && player is null)
        {
            Logger.LogWarning("AC{AC} received from session {Session} with no player assigned",
                ActionCode, session.SessionId);
            return;
        }

        try
        {
            await HandleCoreAsync(session, packet, player, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AC{AC} Sub{Sub} error for {Player}",
                ActionCode, packet.SubAction, player?.CharName ?? session.SessionId);
        }
    }

    /// <summary>
    /// Override to false for handlers that work before player login (AC0, AC63)
    /// </summary>
    protected virtual bool RequiresPlayer => true;

    /// <summary>
    /// Core handler logic — implement in each AC subclass.
    /// </summary>
    protected abstract Task HandleCoreAsync(
        IGameSession session,
        IReceivePacket packet,
        PlayerRuntime? player,
        CancellationToken ct);

    // --- Helper methods for subclasses ---

    /// <summary>Send a built packet to a specific session</summary>
    protected Task SendAsync(IGameSession session, byte[] data)
        => session.SendAsync(data);

    /// <summary>Broadcast to the player's current map</summary>
    protected Task BroadcastToMapAsync(PlayerRuntime player, byte[] data, bool excludeSelf = false)
        => Broadcast.BroadcastToMapAsync(player.MapId, data, excludeSelf ? player.CharId : null);
}
