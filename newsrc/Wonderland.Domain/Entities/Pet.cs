using Wonderland.Domain.Enums;

namespace Wonderland.Domain.Entities;

/// <summary>
/// Pet entity — maps to the 'charpet' table
/// </summary>
public class Pet
{
    public int Id { get; set; }
    public int CharId { get; set; }
    public byte Slot { get; set; }
    public ushort NpcId { get; set; }
    public string Name { get; set; } = string.Empty;

    public byte Level { get; set; } = 1;
    public long TotalExp { get; set; }
    public Affinity Element { get; set; }

    public int MaxHp { get; set; }
    public int MaxSp { get; set; }
    public int CurrentHp { get; set; }
    public int CurrentSp { get; set; }

    public ushort Str { get; set; }
    public ushort Con { get; set; }
    public ushort Agi { get; set; }
    public ushort Int { get; set; }
    public ushort Wis { get; set; }

    public int Intimacy { get; set; }
    public bool IsSummoned { get; set; }

    // Navigation
    public Character? Character { get; set; }
}
