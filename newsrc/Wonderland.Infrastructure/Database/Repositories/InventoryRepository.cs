using Microsoft.EntityFrameworkCore;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Interfaces;

namespace Wonderland.Infrastructure.Database.Repositories;

public class InventoryRepository(WonderlandDbContext db) : BaseRepository<InventoryItem>(db), IInventoryRepository
{
    public async Task<IReadOnlyList<InventoryItem>> GetByCharIdAsync(int charId, CancellationToken ct = default)
        => await DbSet.Where(i => i.CharId == charId).OrderBy(i => i.Slot).ToListAsync(ct);

    public async Task<InventoryItem?> GetBySlotAsync(int charId, byte slot, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(i => i.CharId == charId && i.Slot == slot, ct);

    public async Task<IReadOnlyList<InventoryItem>> GetEquippedAsync(int charId, CancellationToken ct = default)
        => await DbSet.Where(i => i.CharId == charId && i.IsEquipped).ToListAsync(ct);

    public async Task<int> GetItemCountAsync(int charId, CancellationToken ct = default)
        => await DbSet.CountAsync(i => i.CharId == charId && !i.IsEquipped, ct);

    public async Task RemoveBySlotAsync(int charId, byte slot, CancellationToken ct = default)
    {
        var item = await GetBySlotAsync(charId, slot, ct);
        if (item is not null)
            DbSet.Remove(item);
    }
}
