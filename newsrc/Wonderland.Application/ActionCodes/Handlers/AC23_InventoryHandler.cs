using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;

namespace Wonderland.Application.ActionCodes.Handlers;

/// <summary>
/// AC23 — Inventory management.
/// Sub 3:   Drop/destroy item request
/// Sub 5:   Send full inventory state (server→client, triggered on login)
/// Sub 10:  Move item within inventory
/// Sub 11:  Equip item from inventory
/// Sub 12:  Un-equip item to inventory
/// Sub 124: Confirm item destruction
/// </summary>
public class AC23_InventoryHandler : ActionCodeHandlerBase
{
    private const byte MaxInventorySlot = 50;
    private const byte MaxEquipSlot = 6;

    private readonly IInventoryService _inventory;

    public override byte ActionCode => 23;

    public AC23_InventoryHandler(
        IPlayerRuntimeManager players,
        IBroadcastService broadcast,
        IInventoryService inventory,
        ILogger<AC23_InventoryHandler> logger)
        : base(players, broadcast, logger)
    {
        _inventory = inventory;
    }

    protected override async Task HandleCoreAsync(
        IGameSession session, IReceivePacket packet, PlayerRuntime? player, CancellationToken ct)
    {
        if (player is null) return;

        switch (packet.SubAction)
        {
            case 3:
                await HandleDropAsync(session, player, packet, ct);
                break;
            case 10:
                await HandleMoveAsync(session, player, packet, ct);
                break;
            case 11:
                await HandleEquipAsync(session, player, packet, ct);
                break;
            case 12:
                await HandleUnequipAsync(session, player, packet, ct);
                break;
            case 124:
                await HandleDestroyConfirmAsync(session, player, packet, ct);
                break;
            default:
                Logger.LogDebug("AC23 unhandled sub {Sub}", packet.SubAction);
                break;
        }
    }

    /// <summary>
    /// Sub 3: Drop item — [pos:8][qty:8][unk:8]
    /// Non-dropable items trigger destruction confirmation dialog.
    /// </summary>
    private async Task HandleDropAsync(IGameSession session, PlayerRuntime player, IReceivePacket packet, CancellationToken ct)
    {
        var slot = packet.ReadByte();
        var quantity = packet.ReadByte();
        packet.ReadByte(); // unknown

        if (slot is 0 or > MaxInventorySlot) return;

        // For now, treat all drops as destroy requests
        // (ground item system not yet implemented)
        var result = await _inventory.DestroyItemAsync(player.CharId, slot, quantity, ct);
        if (result.Status == InventoryStatus.Success && result.Item is not null)
        {
            await SendAsync(session,
                PacketFactory.Create(23, 9).U8(slot).U8((byte)quantity).Build());
        }
    }

    /// <summary>
    /// Sub 10: Move item — [src:8][qty:8][dst:8]
    /// </summary>
    private async Task HandleMoveAsync(IGameSession session, PlayerRuntime player, IReceivePacket packet, CancellationToken ct)
    {
        var fromSlot = packet.ReadByte();
        var quantity = packet.ReadByte();
        var toSlot = packet.ReadByte();

        if (fromSlot is 0 or > MaxInventorySlot || toSlot is 0 or > MaxInventorySlot) return;
        if (fromSlot == toSlot) return;

        var result = await _inventory.MoveItemAsync(player.CharId, fromSlot, toSlot, quantity, ct);
        if (result.Status == InventoryStatus.Success)
        {
            await SendAsync(session,
                PacketFactory.Create(23, 10).U8(fromSlot).U8((byte)quantity).U8(toSlot).Build());
        }
    }

    /// <summary>
    /// Sub 11: Equip item — [invSlot:8]
    /// </summary>
    private async Task HandleEquipAsync(IGameSession session, PlayerRuntime player, IReceivePacket packet, CancellationToken ct)
    {
        var inventorySlot = packet.ReadByte();
        if (inventorySlot is 0 or > MaxInventorySlot) return;

        var result = await _inventory.EquipItemAsync(player.CharId, inventorySlot, ct);
        if (result.Status == InventoryStatus.Success && result.Item is not null)
        {
            // AC 23,17: Confirm equipped — [invSlot:8][invSlot:8]
            await SendAsync(session,
                PacketFactory.Create(23, 17).U8(inventorySlot).U8(inventorySlot).Build());

            // AC 5,2: Broadcast equipment change to map
            var equipBroadcast = PacketFactory.Create(5, 2)
                .U32((uint)player.CharId)
                .U8(result.EquipSlot)
                .U16(result.Item.ItemId)
                .Build();
            await BroadcastToMapAsync(player, equipBroadcast);
        }
    }

    /// <summary>
    /// Sub 12: Un-equip item — [equipSlot:8][invSlot:8]
    /// </summary>
    private async Task HandleUnequipAsync(IGameSession session, PlayerRuntime player, IReceivePacket packet, CancellationToken ct)
    {
        var equipSlot = packet.ReadByte();
        var targetSlot = packet.ReadByte();

        if (equipSlot is 0 or > MaxEquipSlot) return;
        if (targetSlot is 0 or > MaxInventorySlot) return;

        var result = await _inventory.UnequipItemAsync(player.CharId, equipSlot, targetSlot, ct);
        if (result.Status == InventoryStatus.Success)
        {
            // AC 23,16: Confirm un-equipped — [equipSlot:8][invSlot:8]
            await SendAsync(session,
                PacketFactory.Create(23, 16).U8(equipSlot).U8(targetSlot).Build());

            // AC 5,1: Broadcast equipment removed to map
            var unequipBroadcast = PacketFactory.Create(5, 1)
                .U32((uint)player.CharId)
                .U8(equipSlot)
                .Build();
            await BroadcastToMapAsync(player, unequipBroadcast);
        }
    }

    /// <summary>
    /// Sub 124: Confirm destruction — [pos:8][qty:8][unk:8]
    /// </summary>
    private async Task HandleDestroyConfirmAsync(IGameSession session, PlayerRuntime player, IReceivePacket packet, CancellationToken ct)
    {
        var slot = packet.ReadByte();
        var quantity = packet.ReadByte();
        packet.ReadByte(); // unknown

        if (slot is 0 or > MaxInventorySlot) return;

        var result = await _inventory.DestroyItemAsync(player.CharId, slot, quantity, ct);
        if (result.Status == InventoryStatus.Success && result.Item is not null)
        {
            // AC 23,26: Destroy confirmed — [itemId:16][qty:8]
            await SendAsync(session,
                PacketFactory.Create(23, 26).U16(result.Item.ItemId).U8((byte)quantity).Build());
        }
    }

    /// <summary>
    /// Build and send full inventory state packet (AC 23,5).
    /// Called during login/character select.
    /// </summary>
    public static byte[] BuildInventoryStatePacket(IReadOnlyList<InventoryItem> items)
    {
        var builder = PacketFactory.Create(23, 5);
        foreach (var item in items)
        {
            if (item.ItemId == 0) continue;
            builder
                .U8(item.Slot)
                .U16(item.ItemId)
                .U8((byte)item.Quantity)
                .U8(0) // damage
                .U8((byte)item.Forge)
                .U16(item.SocketId)
                .U16(item.BombId)
                .U16(item.SewId)
                .Pad(17); // remaining enhancement bytes (24 total - 7 used)
        }
        return builder.Build();
    }

    /// <summary>
    /// Build equipment state packet (AC 23,11).
    /// Called during login/character select.
    /// </summary>
    public static byte[] BuildEquipmentStatePacket(IReadOnlyList<InventoryItem> equipped)
    {
        var builder = PacketFactory.Create(23, 11);
        foreach (var item in equipped)
        {
            builder
                .U8(item.EquipSlot)
                .U16(item.ItemId)
                .U8((byte)item.Forge)
                .U16(item.SocketId)
                .U16(item.BombId)
                .U16(item.SewId);
        }
        return builder.Build();
    }
}
