namespace Wonderland.Application.Interfaces;

/// <summary>
/// Base interface for all Action Code packet handlers.
/// Replaces the old AC base class with reflection-based discovery.
/// Each handler processes a specific action code from the game client.
/// </summary>
public interface IActionCodeHandler
{
    /// <summary>
    /// The action code ID this handler processes (e.g., 2 for chat, 6 for movement)
    /// </summary>
    byte ActionCode { get; }

    /// <summary>
    /// Process an incoming packet from a game client session
    /// </summary>
    Task HandleAsync(IGameSession session, IReceivePacket packet, CancellationToken ct = default);
}
