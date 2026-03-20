using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wonderland.Application.ActionCodes;
using Wonderland.Application.Interfaces;
using Wonderland.Infrastructure.Network;

namespace Wonderland.GameServer;

/// <summary>
/// ASP.NET Core hosted service that runs the game server engine.
/// Replaces the old WorldServer with its 3 manual threads.
/// </summary>
public class GameServerHostedService : BackgroundService, IGameServerManager
{
    private readonly GameTcpServer _tcpServer;
    private readonly ActionCodeRouter _router;
    private readonly IPlayerRuntimeManager _playerManager;
    private readonly IWorldEventService _eventService;
    private readonly IConfiguration _config;
    private readonly ILogger<GameServerHostedService> _logger;
    private DateTime? _startedAt;

    public bool IsRunning { get; private set; }
    public int OnlinePlayerCount => _playerManager.OnlineCount;
    public DateTime? StartedAt => _startedAt;

    public GameServerHostedService(
        GameTcpServer tcpServer,
        ActionCodeRouter router,
        IPlayerRuntimeManager playerManager,
        IWorldEventService eventService,
        IConfiguration config,
        ILogger<GameServerHostedService> logger)
    {
        _tcpServer = tcpServer;
        _router = router;
        _playerManager = playerManager;
        _eventService = eventService;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== Wonderland Game Server Starting ===");

        // Wire up TCP events
        _tcpServer.OnPacketReceived += async (session, packet) =>
            await _router.RouteAsync(session, packet, stoppingToken);

        _tcpServer.OnClientConnected += async session =>
        {
            _logger.LogInformation("Game client connected: {SessionId}", session.SessionId);
            await Task.CompletedTask;
        };

        _tcpServer.OnClientDisconnected += async session =>
        {
            // Find and clean up player runtime
            var player = _playerManager.GetBySessionId(session.SessionId);
            if (player is not null)
                _playerManager.Remove(player.CharId);

            _logger.LogInformation("Game client disconnected: {SessionId}", session.SessionId);
            await Task.CompletedTask;
        };

        IsRunning = true;
        _startedAt = DateTime.UtcNow;

        _logger.LogInformation("TCP port: {Port}", _tcpServer.Port);
        _logger.LogInformation("=== Game Server Ready ===");

        // Start TCP listener (blocks until cancelled)
        await _tcpServer.StartAsync(stoppingToken);
    }

    public new Task StartAsync(CancellationToken ct = default)
        => base.StartAsync(ct);

    public new Task StopAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("=== Game Server Shutting Down ===");
        _tcpServer.Stop();
        IsRunning = false;
        return base.StopAsync(ct);
    }

    public ServerStatus GetStatus() => new(
        IsRunning: IsRunning,
        OnlinePlayers: OnlinePlayerCount,
        StartedAt: _startedAt,
        Uptime: _startedAt.HasValue ? DateTime.UtcNow - _startedAt.Value : null,
        TcpPort: _tcpServer.Port,
        ExpMultiplier: _eventService.ExpMultiplier,
        DropMultiplier: _eventService.DropMultiplier);
}
