using Microsoft.EntityFrameworkCore;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Interfaces;

namespace Wonderland.Infrastructure.Database.Repositories;

public class UserRepository(WonderlandDbContext db) : BaseRepository<User>(db), IUserRepository
{
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await GetByUsernameAsync(username, ct);
        return user is not null && user.Password == password && !user.IsBanned;
    }
}
