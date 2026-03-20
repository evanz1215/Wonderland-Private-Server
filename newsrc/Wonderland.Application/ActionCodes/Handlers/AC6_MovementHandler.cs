using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;

namespace Wonderland.Application.ActionCodes.Handlers;

/// <summary>
/// AC6 — Player movement.
/// Sub 1: Direction + position update, broadcast to map.
/// </summary>
public class AC6_MovementHandler : ActionCodeHandlerBase
{
    public override byte ActionCode => 6;

    public AC6_MovementHandler(
        IPlayerRuntimeManager players,
        IBroadcastService broadcast,
        ILogger<AC6_MovementHandler> logger)
        : base(players, broadcast, logger)
    {
    }

    protected override async Task HandleCoreAsync(
        IGameSession session, IReceivePacket packet, PlayerRuntime? player, CancellationToken ct)
    {
        if (player is null) return;

        if (packet.SubAction != 1)
        {
            Logger.LogDebug("AC6 unhandled sub {Sub}", packet.SubAction);
            return;
        }

        // Parse: [direction:8][x:16][y:16]
        var direction = packet.ReadByte();
        var x = packet.ReadUInt16();
        var y = packet.ReadUInt16();

        // Update player position
        player.Direction = direction;
        player.X = x;
        player.Y = y;

        // Broadcast to all players on the same map
        // [6][1][charID:32][direction:8][x:16][y:16]
        var broadcast = PacketFactory.Create(6, 1)
            .U32((uint)player.CharId)
            .U8(direction)
            .U16(x)
            .U16(y)
            .Build();

        await BroadcastToMapAsync(player, broadcast, excludeSelf: true);
    }
}
