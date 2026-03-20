using Wonderland.Domain.Enums;

namespace Wonderland.Domain.Entities;

/// <summary>
/// Static map definition — loaded once from data files.
/// Immutable after initialization.
/// </summary>
public record GameMapDefinition(
    ushort MapId,
    string Name,
    MapType Type,
    ushort BattleBackground = 0
);

/// <summary>
/// Warp portal definition on a map
/// </summary>
public record WarpDestination(
    byte PortalId,
    ushort TargetMapId,
    ushort TargetX,
    ushort TargetY
);

/// <summary>
/// Monster spawn point definition
/// </summary>
public record MonsterSpawn(
    ushort NpcId,
    ushort X,
    ushort Y,
    byte Count,
    int RespawnSeconds
);
