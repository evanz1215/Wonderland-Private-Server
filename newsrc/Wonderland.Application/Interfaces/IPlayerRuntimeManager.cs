using Wonderland.Domain.Entities;

namespace Wonderland.Application.Interfaces;

/// <summary>
/// Manages online PlayerRuntime instances — single source of truth for active players.
/// Thread-safe. All lookups are O(1).
/// </summary>
public interface IPlayerRuntimeManager
{
    int OnlineCount { get; }

    void Add(PlayerRuntime player);
    void Remove(int charId);

    PlayerRuntime? GetByCharId(int charId);
    PlayerRuntime? GetByCharName(string name);
    PlayerRuntime? GetBySessionId(string sessionId);
    IReadOnlyCollection<PlayerRuntime> GetAll();
    IReadOnlyCollection<PlayerRuntime> GetByMapId(ushort mapId);
}
