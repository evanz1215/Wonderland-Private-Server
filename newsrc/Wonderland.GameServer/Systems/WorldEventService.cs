using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;

namespace Wonderland.GameServer.Systems;

public class WorldEventService : IWorldEventService
{
    private readonly ILogger<WorldEventService> _logger;
    private readonly List<ActiveWorldEvent> _activeEvents = [];
    private readonly object _lock = new();

    public double ExpMultiplier { get; private set; } = 1.0;
    public double DropMultiplier { get; private set; } = 1.0;

    public WorldEventService(ILogger<WorldEventService> logger)
    {
        _logger = logger;
    }

    public void StartDoubleExp(TimeSpan duration)
    {
        ExpMultiplier = 2.0;
        var expiresAt = DateTime.UtcNow + duration;

        lock (_lock)
        {
            _activeEvents.RemoveAll(e => e.Name == "Double EXP");
            _activeEvents.Add(new ActiveWorldEvent("Double EXP", 2.0, expiresAt));
        }

        _logger.LogInformation("Double EXP event started, expires at {ExpiresAt}", expiresAt);

        // Schedule expiration
        _ = Task.Delay(duration).ContinueWith(_ =>
        {
            ExpMultiplier = 1.0;
            lock (_lock) { _activeEvents.RemoveAll(e => e.Name == "Double EXP"); }
            _logger.LogInformation("Double EXP event ended");
        });
    }

    public void StartDoubleDrop(TimeSpan duration)
    {
        DropMultiplier = 2.0;
        var expiresAt = DateTime.UtcNow + duration;

        lock (_lock)
        {
            _activeEvents.RemoveAll(e => e.Name == "Double Drop");
            _activeEvents.Add(new ActiveWorldEvent("Double Drop", 2.0, expiresAt));
        }

        _logger.LogInformation("Double Drop event started, expires at {ExpiresAt}", expiresAt);

        _ = Task.Delay(duration).ContinueWith(_ =>
        {
            DropMultiplier = 1.0;
            lock (_lock) { _activeEvents.RemoveAll(e => e.Name == "Double Drop"); }
            _logger.LogInformation("Double Drop event ended");
        });
    }

    public void StopAllEvents()
    {
        ExpMultiplier = 1.0;
        DropMultiplier = 1.0;
        lock (_lock) { _activeEvents.Clear(); }
        _logger.LogInformation("All world events stopped");
    }

    public IReadOnlyList<ActiveWorldEvent> GetActiveEvents()
    {
        lock (_lock) { return _activeEvents.ToList().AsReadOnly(); }
    }
}
