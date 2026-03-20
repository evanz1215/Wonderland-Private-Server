using Wonderland.Domain.Enums;

namespace Wonderland.Domain.Entities;

/// <summary>
/// Player character entity — maps to the 'characters' table
/// </summary>
public class Character
{
    public int CharId { get; set; }
    public int UserId { get; set; }
    public byte Slot { get; set; }
    public string Name { get; set; } = string.Empty;

    // Appearance
    public ushort Body { get; set; }
    public ushort Head { get; set; }
    public ushort Hair { get; set; }
    public ushort Eyes { get; set; }
    public ushort Skin { get; set; }
    public ushort Clothing { get; set; }

    // Location
    public int MapId { get; set; }
    public ushort X { get; set; }
    public ushort Y { get; set; }

    // Stats
    public byte Level { get; set; } = 1;
    public long TotalExp { get; set; }
    public int Gold { get; set; }

    public int MaxHp { get; set; }
    public int MaxSp { get; set; }
    public int CurrentHp { get; set; }
    public int CurrentSp { get; set; }

    public ushort Str { get; set; }
    public ushort Con { get; set; }
    public ushort Agi { get; set; }
    public ushort Int { get; set; }
    public ushort Wis { get; set; }
    public ushort PotentialPoints { get; set; }
    public ushort SkillPoints { get; set; }

    // Reborn
    public RebornJob RebornJob { get; set; }
    public bool IsReborn { get; set; }

    // Element
    public Affinity Affinity { get; set; }

    // Navigation
    public User? User { get; set; }
    public ICollection<InventoryItem> Inventory { get; set; } = [];
    public ICollection<Pet> Pets { get; set; } = [];
}
