using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;

namespace Wonderland.Application.ActionCodes.Handlers;

/// <summary>
/// AC0 — Server version handshake.
/// Sent when client first connects. Responds with server version and equipment slot info.
/// </summary>
public class AC0_VersionHandler : ActionCodeHandlerBase
{
    private const ushort ServerVersion = 1096;
    private const byte EquipSlotCount = 6;

    public override byte ActionCode => 0;
    protected override bool RequiresPlayer => false; // Pre-login handler

    public AC0_VersionHandler(
        IPlayerRuntimeManager players,
        IBroadcastService broadcast,
        ILogger<AC0_VersionHandler> logger)
        : base(players, broadcast, logger)
    {
    }

    protected override async Task HandleCoreAsync(
        IGameSession session, IReceivePacket packet, PlayerRuntime? player, CancellationToken ct)
    {
        Logger.LogDebug("AC0 version handshake from {Session}", session.SessionId);

        // Response: [0][serverVersion:16][equipSlots:8]
        var response = PacketFactory.Create(0)
            .U16(ServerVersion)
            .U8(EquipSlotCount)
            .Build();

        await SendAsync(session, response);
    }
}
