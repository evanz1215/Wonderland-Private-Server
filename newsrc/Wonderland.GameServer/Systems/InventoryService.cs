using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Interfaces;

namespace Wonderland.GameServer.Systems;

/// <summary>
/// Manages inventory operations: add/remove/move items, equip/un-equip.
/// All operations are persisted to database.
/// </summary>
public class InventoryService : IInventoryService
{
    private const byte MaxSlot = 50;
    private const byte MaxEquipSlot = 6;

    private readonly IInventoryRepository _repo;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(IInventoryRepository repo, ILogger<InventoryService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(int charId, CancellationToken ct = default)
        => await _repo.GetByCharIdAsync(charId, ct);

    public async Task<IReadOnlyList<InventoryItem>> GetEquippedAsync(int charId, CancellationToken ct = default)
        => await _repo.GetEquippedAsync(charId, ct);

    public async Task<InventoryResult> AddItemAsync(int charId, ushort itemId, ushort quantity = 1, CancellationToken ct = default)
    {
        // Find first empty slot
        var emptySlot = await FindEmptySlotAsync(charId, ct);
        if (emptySlot is null)
            return new InventoryResult(InventoryStatus.InventoryFull);

        var item = new InventoryItem
        {
            CharId = charId,
            Slot = emptySlot.Value,
            ItemId = itemId,
            Quantity = quantity,
        };

        try
        {
            await _repo.AddAsync(item, ct);
            await _repo.SaveChangesAsync(ct);
            _logger.LogDebug("Added item {ItemId} x{Qty} to char {CharId} slot {Slot}",
                itemId, quantity, charId, emptySlot.Value);
            return new InventoryResult(InventoryStatus.Success, item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add item {ItemId} for char {CharId}", itemId, charId);
            return new InventoryResult(InventoryStatus.Failed, ErrorMessage: ex.Message);
        }
    }

    public async Task<InventoryResult> RemoveItemAsync(int charId, byte slot, ushort quantity = 1, CancellationToken ct = default)
    {
        if (slot is 0 or > MaxSlot)
            return new InventoryResult(InventoryStatus.InvalidSlot);

        var item = await _repo.GetBySlotAsync(charId, slot, ct);
        if (item is null)
            return new InventoryResult(InventoryStatus.SlotEmpty);

        if (item.Quantity < quantity)
            return new InventoryResult(InventoryStatus.InsufficientQuantity);

        try
        {
            if (item.Quantity == quantity)
            {
                await _repo.DeleteAsync(item, ct);
            }
            else
            {
                item.Quantity -= quantity;
                await _repo.UpdateAsync(item, ct);
            }

            await _repo.SaveChangesAsync(ct);
            _logger.LogDebug("Removed {Qty} of item {ItemId} from char {CharId} slot {Slot}",
                quantity, item.ItemId, charId, slot);
            return new InventoryResult(InventoryStatus.Success, item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove item from char {CharId} slot {Slot}", charId, slot);
            return new InventoryResult(InventoryStatus.Failed, ErrorMessage: ex.Message);
        }
    }

    public async Task<InventoryResult> MoveItemAsync(int charId, byte fromSlot, byte toSlot, ushort quantity = 1, CancellationToken ct = default)
    {
        if (fromSlot is 0 or > MaxSlot || toSlot is 0 or > MaxSlot)
            return new InventoryResult(InventoryStatus.InvalidSlot);

        var sourceItem = await _repo.GetBySlotAsync(charId, fromSlot, ct);
        if (sourceItem is null)
            return new InventoryResult(InventoryStatus.SlotEmpty);

        var destItem = await _repo.GetBySlotAsync(charId, toSlot, ct);

        try
        {
            if (destItem is not null)
            {
                // Swap: destination has an item
                destItem.Slot = fromSlot;
                sourceItem.Slot = toSlot;
                await _repo.UpdateAsync(sourceItem, ct);
                await _repo.UpdateAsync(destItem, ct);
            }
            else
            {
                // Move to empty slot
                if (quantity >= sourceItem.Quantity)
                {
                    sourceItem.Slot = toSlot;
                    await _repo.UpdateAsync(sourceItem, ct);
                }
                else
                {
                    // Split stack
                    sourceItem.Quantity -= quantity;
                    await _repo.UpdateAsync(sourceItem, ct);

                    var newItem = new InventoryItem
                    {
                        CharId = charId,
                        Slot = toSlot,
                        ItemId = sourceItem.ItemId,
                        Quantity = quantity,
                        Forge = sourceItem.Forge,
                        SocketId = sourceItem.SocketId,
                        BombId = sourceItem.BombId,
                        SewId = sourceItem.SewId,
                    };
                    await _repo.AddAsync(newItem, ct);
                }
            }

            await _repo.SaveChangesAsync(ct);
            _logger.LogDebug("Moved item from slot {From} to {To} for char {CharId}", fromSlot, toSlot, charId);
            return new InventoryResult(InventoryStatus.Success, sourceItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move item for char {CharId}", charId);
            return new InventoryResult(InventoryStatus.Failed, ErrorMessage: ex.Message);
        }
    }

    public async Task<EquipResult> EquipItemAsync(int charId, byte inventorySlot, CancellationToken ct = default)
    {
        if (inventorySlot is 0 or > MaxSlot)
            return new EquipResult(InventoryStatus.InvalidSlot);

        var item = await _repo.GetBySlotAsync(charId, inventorySlot, ct);
        if (item is null)
            return new EquipResult(InventoryStatus.SlotEmpty);

        if (item.EquipSlot == 0)
            return new EquipResult(InventoryStatus.ItemNotEquippable);

        var targetEquipSlot = item.EquipSlot;

        // Check if something is already equipped in that slot
        var equipped = await _repo.GetEquippedAsync(charId, ct);
        var existingEquip = equipped.FirstOrDefault(e => e.EquipSlot == targetEquipSlot);
        if (existingEquip is not null)
        {
            // Swap: un-equip existing, equip new
            existingEquip.IsEquipped = false;
            existingEquip.Slot = inventorySlot; // Move old equip to the freed slot
            existingEquip.EquipSlot = 0;
            await _repo.UpdateAsync(existingEquip, ct);
        }

        item.IsEquipped = true;
        item.EquipSlot = targetEquipSlot;
        await _repo.UpdateAsync(item, ct);

        try
        {
            await _repo.SaveChangesAsync(ct);
            _logger.LogDebug("Equipped item {ItemId} in slot {EquipSlot} for char {CharId}",
                item.ItemId, targetEquipSlot, charId);
            return new EquipResult(InventoryStatus.Success, item, targetEquipSlot, inventorySlot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to equip item for char {CharId}", charId);
            return new EquipResult(InventoryStatus.Failed, ErrorMessage: ex.Message);
        }
    }

    public async Task<EquipResult> UnequipItemAsync(int charId, byte equipSlot, byte targetInventorySlot, CancellationToken ct = default)
    {
        if (equipSlot is 0 or > MaxEquipSlot)
            return new EquipResult(InventoryStatus.InvalidSlot);
        if (targetInventorySlot is 0 or > MaxSlot)
            return new EquipResult(InventoryStatus.InvalidSlot);

        // Find equipped item
        var equipped = await _repo.GetEquippedAsync(charId, ct);
        var item = equipped.FirstOrDefault(e => e.EquipSlot == equipSlot);
        if (item is null)
            return new EquipResult(InventoryStatus.SlotEmpty);

        // Check target slot is free
        var targetItem = await _repo.GetBySlotAsync(charId, targetInventorySlot, ct);
        if (targetItem is not null && targetItem.Id != item.Id)
            return new EquipResult(InventoryStatus.SlotOccupied);

        item.IsEquipped = false;
        item.Slot = targetInventorySlot;
        item.EquipSlot = 0;
        await _repo.UpdateAsync(item, ct);

        try
        {
            await _repo.SaveChangesAsync(ct);
            _logger.LogDebug("Unequipped item {ItemId} from slot {EquipSlot} for char {CharId}",
                item.ItemId, equipSlot, charId);
            return new EquipResult(InventoryStatus.Success, item, equipSlot, targetInventorySlot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unequip item for char {CharId}", charId);
            return new EquipResult(InventoryStatus.Failed, ErrorMessage: ex.Message);
        }
    }

    public async Task<InventoryResult> DestroyItemAsync(int charId, byte slot, ushort quantity = 1, CancellationToken ct = default)
    {
        // Reuse RemoveItem — destruction is logically the same
        return await RemoveItemAsync(charId, slot, quantity, ct);
    }

    public async Task<byte?> FindEmptySlotAsync(int charId, CancellationToken ct = default)
    {
        var items = await _repo.GetByCharIdAsync(charId, ct);
        var usedSlots = items.Where(i => !i.IsEquipped).Select(i => i.Slot).ToHashSet();

        for (byte slot = 1; slot <= MaxSlot; slot++)
        {
            if (!usedSlots.Contains(slot))
                return slot;
        }

        return null;
    }
}
