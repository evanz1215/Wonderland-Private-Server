using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;

namespace Wonderland.GameServer.Systems;

/// <summary>
/// Manages game maps — registration, player enter/leave, warp routing.
/// </summary>
public class MapManager : IMapManager
{
    private readonly ConcurrentDictionary<ushort, GameMapDefinition> _maps = new();
    private readonly ConcurrentDictionary<ushort, List<WarpDestination>> _warps = new();
    private readonly IBroadcastService _broadcast;
    private readonly IPlayerRuntimeManager _players;
    private readonly ILogger<MapManager> _logger;

    public MapManager(
        IBroadcastService broadcast,
        IPlayerRuntimeManager players,
        ILogger<MapManager> logger)
    {
        _broadcast = broadcast;
        _players = players;
        _logger = logger;
    }

    public void RegisterMap(GameMapDefinition map)
    {
        _maps.TryAdd(map.MapId, map);
        _warps.TryAdd(map.MapId, []);
        _logger.LogDebug("Map registered: {MapId} ({Name})", map.MapId, map.Name);
    }

    public GameMapDefinition? GetMap(ushort mapId)
        => _maps.GetValueOrDefault(mapId);

    public IReadOnlyCollection<GameMapDefinition> GetAllMaps()
        => _maps.Values.ToList().AsReadOnly();

    public async Task PlayerEnterMapAsync(PlayerRuntime player, ushort mapId, ushort x, ushort y)
    {
        player.MapId = mapId;
        player.X = x;
        player.Y = y;

        // Broadcast character appearance to all players on the new map
        var enterPacket = Application.ActionCodes.PacketFactory.Create(3)
            .U32((uint)player.CharId)
            .U8((byte)player.Body)
            .U16(mapId)
            .U16(x)
            .U16(y)
            .U8(0)
            .U16(player.Head)
            .U16(player.Hair)
            .U16(player.Skin)
            .U16(player.Clothing)
            .U16(player.Eyes)
            .U8(0) // equip count
            .Pad(4)
            .NStr(player.CharName)
            .NStr("")
            .U8(0)
            .Build();

        await _broadcast.BroadcastToMapAsync(mapId, enterPacket, excludeCharId: player.CharId);
    }

    public async Task PlayerLeaveMapAsync(PlayerRuntime player)
    {
        // Notify map that player left: [5][7][charID:32]
        var leavePacket = Application.ActionCodes.PacketFactory.Create(5, 7)
            .U32((uint)player.CharId)
            .Build();

        await _broadcast.BroadcastToMapAsync(player.MapId, leavePacket, excludeCharId: player.CharId);
    }

    public async Task TeleportPlayerAsync(PlayerRuntime player, ushort targetMapId, ushort x, ushort y)
    {
        // Leave current map
        await PlayerLeaveMapAsync(player);

        // Enter new map
        await PlayerEnterMapAsync(player, targetMapId, x, y);

        // Send warp packet to the player: [5][4][mapID:16][x:16][y:16]
        var warpPacket = Application.ActionCodes.PacketFactory.Create(5, 4)
            .U16(targetMapId)
            .U16(x)
            .U16(y)
            .Build();

        await _broadcast.SendToPlayerAsync(player.CharId, warpPacket);
    }

    public IReadOnlyList<WarpDestination> GetWarpDestinations(ushort mapId)
        => _warps.TryGetValue(mapId, out var warps) ? warps.AsReadOnly() : [];

    public void AddWarpDestination(ushort mapId, WarpDestination warp)
    {
        _warps.AddOrUpdate(mapId, [warp], (_, list) => { list.Add(warp); return list; });
    }
}
