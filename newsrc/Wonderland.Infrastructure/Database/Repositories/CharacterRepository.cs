using Microsoft.EntityFrameworkCore;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Interfaces;

namespace Wonderland.Infrastructure.Database.Repositories;

public class CharacterRepository(WonderlandDbContext db) : BaseRepository<Character>(db), ICharacterRepository
{
    public async Task<IReadOnlyList<Character>> GetByUserIdAsync(int userId, CancellationToken ct = default)
        => await DbSet.Where(c => c.UserId == userId).ToListAsync(ct);

    public async Task<Character?> GetByNameAsync(string name, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(c => c.Name == name, ct);

    public async Task<bool> IsNameTakenAsync(string name, CancellationToken ct = default)
        => await DbSet.AnyAsync(c => EF.Functions.ILike(c.Name, name), ct);

    public async Task<int> GetCharacterCountAsync(int userId, CancellationToken ct = default)
        => await DbSet.CountAsync(c => c.UserId == userId, ct);

    public async Task<Character?> GetWithInventoryAsync(int charId, CancellationToken ct = default)
        => await DbSet.Include(c => c.Inventory).FirstOrDefaultAsync(c => c.CharId == charId, ct);

    public async Task<Character?> GetWithPetsAsync(int charId, CancellationToken ct = default)
        => await DbSet.Include(c => c.Pets).FirstOrDefaultAsync(c => c.CharId == charId, ct);
}
