using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;

namespace Wonderland.Application.ActionCodes;

/// <summary>
/// Routes incoming packets to the correct IActionCodeHandler based on action code ID.
/// Replaces the old AC.GetAction() reflection-based lookup.
/// </summary>
public class ActionCodeRouter
{
    private readonly Dictionary<byte, IActionCodeHandler> _handlers;
    private readonly ILogger<ActionCodeRouter> _logger;

    public ActionCodeRouter(
        IEnumerable<IActionCodeHandler> handlers,
        ILogger<ActionCodeRouter> logger)
    {
        _logger = logger;
        _handlers = handlers.ToDictionary(h => h.ActionCode, h => h);
        _logger.LogInformation("Registered {Count} action code handlers", _handlers.Count);
    }

    public async Task RouteAsync(IGameSession session, IReceivePacket packet, CancellationToken ct = default)
    {
        if (_handlers.TryGetValue(packet.ActionCode, out var handler))
        {
            try
            {
                await handler.HandleAsync(session, packet, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling AC{ActionCode} sub{SubAction} from {Session}",
                    packet.ActionCode, packet.SubAction, session.SessionId);
            }
        }
        else
        {
            _logger.LogDebug("Unhandled AC{ActionCode} from {Session}", packet.ActionCode, session.SessionId);
        }
    }
}
