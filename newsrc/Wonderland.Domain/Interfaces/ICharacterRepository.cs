using Wonderland.Domain.Entities;

namespace Wonderland.Domain.Interfaces;

public interface ICharacterRepository : IRepository<Character>
{
    Task<IReadOnlyList<Character>> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<Character?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<bool> IsNameTakenAsync(string name, CancellationToken ct = default);
    Task<int> GetCharacterCountAsync(int userId, CancellationToken ct = default);
    Task<Character?> GetWithInventoryAsync(int charId, CancellationToken ct = default);
    Task<Character?> GetWithPetsAsync(int charId, CancellationToken ct = default);
}
