using System.Collections.Concurrent;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;

namespace Wonderland.GameServer.Systems;

/// <summary>
/// Thread-safe in-memory store of all online players.
/// O(1) lookup by CharId, CharName, SessionId, and MapId.
/// </summary>
public class PlayerRuntimeManager : IPlayerRuntimeManager
{
    private readonly ConcurrentDictionary<int, PlayerRuntime> _byCharId = new();
    private readonly ConcurrentDictionary<string, int> _nameToCharId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _sessionToCharId = new();

    public int OnlineCount => _byCharId.Count;

    public void Add(PlayerRuntime player)
    {
        _byCharId.TryAdd(player.CharId, player);
        _nameToCharId.TryAdd(player.CharName, player.CharId);
        _sessionToCharId.TryAdd(player.SessionId, player.CharId);
    }

    public void Remove(int charId)
    {
        if (_byCharId.TryRemove(charId, out var player))
        {
            _nameToCharId.TryRemove(player.CharName, out _);
            _sessionToCharId.TryRemove(player.SessionId, out _);
        }
    }

    public PlayerRuntime? GetByCharId(int charId)
        => _byCharId.GetValueOrDefault(charId);

    public PlayerRuntime? GetByCharName(string name)
        => _nameToCharId.TryGetValue(name, out var charId) ? GetByCharId(charId) : null;

    public PlayerRuntime? GetBySessionId(string sessionId)
        => _sessionToCharId.TryGetValue(sessionId, out var charId) ? GetByCharId(charId) : null;

    public IReadOnlyCollection<PlayerRuntime> GetAll()
        => _byCharId.Values.ToList().AsReadOnly();

    public IReadOnlyCollection<PlayerRuntime> GetByMapId(ushort mapId)
        => _byCharId.Values.Where(p => p.MapId == mapId).ToList().AsReadOnly();
}
