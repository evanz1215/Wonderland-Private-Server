using Wonderland.Domain.Entities;

namespace Wonderland.Application.Interfaces;

/// <summary>
/// Manages player inventory operations: add/remove/move items, equip/un-equip.
/// </summary>
public interface IInventoryService
{
    /// <summary>Load all inventory items for a character.</summary>
    Task<IReadOnlyList<InventoryItem>> GetInventoryAsync(int charId, CancellationToken ct = default);

    /// <summary>Load equipped items for a character.</summary>
    Task<IReadOnlyList<InventoryItem>> GetEquippedAsync(int charId, CancellationToken ct = default);

    /// <summary>Add an item to inventory. Returns the created item or null if inventory full.</summary>
    Task<InventoryResult> AddItemAsync(int charId, ushort itemId, ushort quantity = 1, CancellationToken ct = default);

    /// <summary>Remove quantity from a slot. Returns the removed item info.</summary>
    Task<InventoryResult> RemoveItemAsync(int charId, byte slot, ushort quantity = 1, CancellationToken ct = default);

    /// <summary>Move item from one slot to another.</summary>
    Task<InventoryResult> MoveItemAsync(int charId, byte fromSlot, byte toSlot, ushort quantity = 1, CancellationToken ct = default);

    /// <summary>Equip an item from inventory slot to equipment slot.</summary>
    Task<EquipResult> EquipItemAsync(int charId, byte inventorySlot, CancellationToken ct = default);

    /// <summary>Un-equip from equipment slot back to inventory.</summary>
    Task<EquipResult> UnequipItemAsync(int charId, byte equipSlot, byte targetInventorySlot, CancellationToken ct = default);

    /// <summary>Destroy an item completely.</summary>
    Task<InventoryResult> DestroyItemAsync(int charId, byte slot, ushort quantity = 1, CancellationToken ct = default);

    /// <summary>Find the first empty slot in inventory (1-50).</summary>
    Task<byte?> FindEmptySlotAsync(int charId, CancellationToken ct = default);
}

public record InventoryResult(
    InventoryStatus Status,
    InventoryItem? Item = null,
    string? ErrorMessage = null
);

public record EquipResult(
    InventoryStatus Status,
    InventoryItem? Item = null,
    byte EquipSlot = 0,
    byte InventorySlot = 0,
    string? ErrorMessage = null
);

public enum InventoryStatus
{
    Success,
    InventoryFull,
    SlotEmpty,
    SlotOccupied,
    InvalidSlot,
    InsufficientQuantity,
    ItemNotEquippable,
    Failed,
}
