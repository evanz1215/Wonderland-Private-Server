using Wonderland.Domain.Entities;

namespace Wonderland.Domain.Interfaces;

public interface IInventoryRepository : IRepository<InventoryItem>
{
    Task<IReadOnlyList<InventoryItem>> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<InventoryItem?> GetBySlotAsync(int charId, byte slot, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryItem>> GetEquippedAsync(int charId, CancellationToken ct = default);
    Task<int> GetItemCountAsync(int charId, CancellationToken ct = default);
    Task RemoveBySlotAsync(int charId, byte slot, CancellationToken ct = default);
}
