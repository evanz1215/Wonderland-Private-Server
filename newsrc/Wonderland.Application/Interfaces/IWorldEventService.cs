namespace Wonderland.Application.Interfaces;

/// <summary>
/// Manages world events — exp multiplier, drop rate, etc.
/// </summary>
public interface IWorldEventService
{
    double ExpMultiplier { get; }
    double DropMultiplier { get; }

    void StartDoubleExp(TimeSpan duration);
    void StartDoubleDrop(TimeSpan duration);
    void StopAllEvents();
    IReadOnlyList<ActiveWorldEvent> GetActiveEvents();
}

public record ActiveWorldEvent(string Name, double Multiplier, DateTime ExpiresAt);
