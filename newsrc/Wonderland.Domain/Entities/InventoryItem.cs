using Wonderland.Domain.Enums;

namespace Wonderland.Domain.Entities;

/// <summary>
/// Inventory item entity — maps to the 'inventory' table
/// </summary>
public class InventoryItem
{
    public int Id { get; set; }
    public int CharId { get; set; }
    public byte Slot { get; set; }
    public ushort ItemId { get; set; }
    public ushort Quantity { get; set; } = 1;

    // Equipment enhancement
    public ForgeLevel Forge { get; set; }
    public ushort SocketId { get; set; }
    public ushort BombId { get; set; }
    public ushort SewId { get; set; }

    // Equipment flag — true if this item is worn in an equip slot
    public bool IsEquipped { get; set; }
    public byte EquipSlot { get; set; }

    // Navigation
    public Character? Character { get; set; }
}
