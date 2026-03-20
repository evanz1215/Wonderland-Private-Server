namespace Wonderland.Application.Interfaces;

/// <summary>
/// Manages the overall game server state — start, stop, status
/// </summary>
public interface IGameServerManager
{
    bool IsRunning { get; }
    int OnlinePlayerCount { get; }
    DateTime? StartedAt { get; }

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    ServerStatus GetStatus();
}

public record ServerStatus(
    bool IsRunning,
    int OnlinePlayers,
    DateTime? StartedAt,
    TimeSpan? Uptime,
    int TcpPort,
    double ExpMultiplier,
    double DropMultiplier);
