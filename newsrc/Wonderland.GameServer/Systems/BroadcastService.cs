using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;
using Wonderland.Infrastructure.Network;

namespace Wonderland.GameServer.Systems;

/// <summary>
/// Sends packets to players via the TCP server.
/// Bridges Application layer abstractions to Infrastructure layer transport.
/// </summary>
public class BroadcastService : IBroadcastService
{
    private readonly GameTcpServer _tcpServer;
    private readonly IPlayerRuntimeManager _players;
    private readonly ILogger<BroadcastService> _logger;

    public BroadcastService(
        GameTcpServer tcpServer,
        IPlayerRuntimeManager players,
        ILogger<BroadcastService> logger)
    {
        _tcpServer = tcpServer;
        _players = players;
        _logger = logger;
    }

    public async Task SendToSessionAsync(string sessionId, byte[] data)
    {
        await _tcpServer.BroadcastAsync(data, s => s.SessionId == sessionId);
    }

    public async Task SendToPlayerAsync(int charId, byte[] data)
    {
        var player = _players.GetByCharId(charId);
        if (player is not null)
            await SendToSessionAsync(player.SessionId, data);
    }

    public async Task BroadcastToMapAsync(ushort mapId, byte[] data, int? excludeCharId = null)
    {
        var playersOnMap = _players.GetByMapId(mapId);
        foreach (var p in playersOnMap)
        {
            if (excludeCharId.HasValue && p.CharId == excludeCharId.Value) continue;
            await SendToSessionAsync(p.SessionId, data);
        }
    }

    public async Task BroadcastToAllAsync(byte[] data)
    {
        await _tcpServer.BroadcastAsync(data);
    }

    public async Task BroadcastToTeamAsync(int teamId, byte[] data)
    {
        // Find all players in the team
        var teamPlayers = _players.GetAll().Where(p => p.TeamId == teamId);
        foreach (var p in teamPlayers)
        {
            await SendToSessionAsync(p.SessionId, data);
        }
    }
}
