using Microsoft.EntityFrameworkCore;
using Wonderland.Domain.Interfaces;

namespace Wonderland.Infrastructure.Database.Repositories;

public class BaseRepository<T>(WonderlandDbContext db) : IRepository<T> where T : class
{
    protected readonly WonderlandDbContext Db = db;
    protected readonly DbSet<T> DbSet = db.Set<T>();

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await DbSet.ToListAsync(ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await DbSet.AddAsync(entity, ct);

    public virtual Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task SaveChangesAsync(CancellationToken ct = default)
        => await Db.SaveChangesAsync(ct);
}
