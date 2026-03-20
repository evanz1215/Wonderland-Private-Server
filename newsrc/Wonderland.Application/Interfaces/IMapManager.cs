using Wonderland.Domain.Entities;

namespace Wonderland.Application.Interfaces;

/// <summary>
/// Manages game maps — player enter/leave, map lookup, warp routing.
/// </summary>
public interface IMapManager
{
    /// <summary>Register a map definition</summary>
    void RegisterMap(GameMapDefinition map);

    /// <summary>Get map definition by ID</summary>
    GameMapDefinition? GetMap(ushort mapId);

    /// <summary>Get all registered maps</summary>
    IReadOnlyCollection<GameMapDefinition> GetAllMaps();

    /// <summary>Player enters a map</summary>
    Task PlayerEnterMapAsync(PlayerRuntime player, ushort mapId, ushort x, ushort y);

    /// <summary>Player leaves current map</summary>
    Task PlayerLeaveMapAsync(PlayerRuntime player);

    /// <summary>Teleport player to a new map + position</summary>
    Task TeleportPlayerAsync(PlayerRuntime player, ushort targetMapId, ushort x, ushort y);

    /// <summary>Get warp destinations for a map</summary>
    IReadOnlyList<WarpDestination> GetWarpDestinations(ushort mapId);
}
