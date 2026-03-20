using Wonderland.Domain.Entities;

namespace Wonderland.Application.Interfaces;

/// <summary>
/// Sends packets to groups of players. Abstracts away the transport layer.
/// All methods are fire-and-forget safe.
/// </summary>
public interface IBroadcastService
{
    /// <summary>Send raw packet data to a specific player by session ID</summary>
    Task SendToSessionAsync(string sessionId, byte[] data);

    /// <summary>Send to a specific player by char ID</summary>
    Task SendToPlayerAsync(int charId, byte[] data);

    /// <summary>Broadcast to all players on a specific map</summary>
    Task BroadcastToMapAsync(ushort mapId, byte[] data, int? excludeCharId = null);

    /// <summary>Broadcast to all online players (system announcements)</summary>
    Task BroadcastToAllAsync(byte[] data);

    /// <summary>Broadcast to a specific team</summary>
    Task BroadcastToTeamAsync(int teamId, byte[] data);
}
