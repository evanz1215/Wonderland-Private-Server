using System.Collections.Concurrent;
using Wonderland.Domain.Enums;

namespace Wonderland.Domain.Entities;

/// <summary>
/// Runtime player state — lives in memory while the player is online.
/// Bridges the persistent Character entity with the active game session.
/// Thread-safe for concurrent access from game loop, packet handlers, and broadcast.
/// </summary>
public class PlayerRuntime
{
    private readonly object _lock = new();

    // Identity
    public int UserId { get; init; }
    public int CharId { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public string CharName { get; init; } = string.Empty;
    public GmStatus GmLevel { get; init; }

    // Account
    public string CipherPassword { get; init; } = string.Empty;
    public int ImPoints { get; set; }

    // Position (volatile — updated by movement packets)
    private ushort _mapId;
    private ushort _x;
    private ushort _y;
    private byte _direction;

    public ushort MapId { get => _mapId; set { lock (_lock) _mapId = value; } }
    public ushort X { get => _x; set { lock (_lock) _x = value; } }
    public ushort Y { get => _y; set { lock (_lock) _y = value; } }
    public byte Direction { get => _direction; set { lock (_lock) _direction = value; } }

    // Appearance
    public ushort Body { get; init; }
    public ushort Head { get; init; }
    public ushort Hair { get; init; }
    public ushort Eyes { get; init; }
    public ushort Skin { get; init; }
    public ushort Clothing { get; init; }

    // Combat stats
    public byte Level { get; set; }
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
    public Affinity Affinity { get; set; }

    // Flags
    private readonly HashSet<PlayerFlag> _flags = [];
    public void AddFlag(PlayerFlag flag) { lock (_lock) _flags.Add(flag); }
    public void RemoveFlag(PlayerFlag flag) { lock (_lock) _flags.Remove(flag); }
    public bool HasFlag(PlayerFlag flag) { lock (_lock) return _flags.Contains(flag); }

    // Mute
    public DateTime MuteUntil { get; set; } = DateTime.MinValue;
    public bool IsMuted => DateTime.UtcNow < MuteUntil;

    // Battle reference
    public ushort? BattleId { get; set; }
    public bool IsInBattle => BattleId.HasValue;

    // Team reference
    public int? TeamId { get; set; }

    /// <summary>
    /// Creates a PlayerRuntime from a persistent Character entity
    /// </summary>
    public static PlayerRuntime FromCharacter(Character c, User u, string sessionId) => new()
    {
        UserId = u.Id,
        CharId = c.CharId,
        SessionId = sessionId,
        CharName = c.Name,
        GmLevel = u.GmLevel,
        CipherPassword = u.CipherPassword,
        ImPoints = u.ImPoints,
        MapId = (ushort)c.MapId,
        X = c.X,
        Y = c.Y,
        Body = c.Body,
        Head = c.Head,
        Hair = c.Hair,
        Eyes = c.Eyes,
        Skin = c.Skin,
        Clothing = c.Clothing,
        Level = c.Level,
        TotalExp = c.TotalExp,
        Gold = c.Gold,
        MaxHp = c.MaxHp,
        MaxSp = c.MaxSp,
        CurrentHp = c.CurrentHp,
        CurrentSp = c.CurrentSp,
        Str = c.Str,
        Con = c.Con,
        Agi = c.Agi,
        Int = c.Int,
        Wis = c.Wis,
        PotentialPoints = c.PotentialPoints,
        SkillPoints = c.SkillPoints,
        RebornJob = c.RebornJob,
        IsReborn = c.IsReborn,
        Affinity = c.Affinity,
    };

    /// <summary>
    /// Syncs runtime state back to the persistent Character entity for DB save
    /// </summary>
    public void SyncToCharacter(Character c)
    {
        c.MapId = MapId;
        c.X = X;
        c.Y = Y;
        c.Level = Level;
        c.TotalExp = TotalExp;
        c.Gold = Gold;
        c.MaxHp = MaxHp;
        c.MaxSp = MaxSp;
        c.CurrentHp = CurrentHp;
        c.CurrentSp = CurrentSp;
        c.Str = Str;
        c.Con = Con;
        c.Agi = Agi;
        c.Int = Int;
        c.Wis = Wis;
        c.PotentialPoints = PotentialPoints;
        c.SkillPoints = SkillPoints;
        c.RebornJob = RebornJob;
        c.IsReborn = IsReborn;
        c.Affinity = Affinity;
    }
}
