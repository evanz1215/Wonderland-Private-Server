using Wonderland.Domain.Entities;

namespace Wonderland.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken ct = default);
}
