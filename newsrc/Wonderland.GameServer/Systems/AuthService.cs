using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Interfaces;

namespace Wonderland.GameServer.Systems;

/// <summary>
/// Authentication service — validates credentials, manages character loading.
/// </summary>
public class AuthService : IAuthService
{
    private const ushort MinClientVersion = 1096;

    private readonly IUserRepository _userRepo;
    private readonly ICharacterRepository _characterRepo;
    private readonly IPlayerRuntimeManager _players;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepo,
        ICharacterRepository characterRepo,
        IPlayerRuntimeManager players,
        ILogger<AuthService> logger)
    {
        _userRepo = userRepo;
        _characterRepo = characterRepo;
        _players = players;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(
        string username, string password, ushort clientVersion, CancellationToken ct = default)
    {
        // Version check
        if (clientVersion < MinClientVersion)
            return new AuthResult(AuthStatus.VersionMismatch, ErrorMessage: "Client version too old");

        // Input validation
        if (username.Length is < 4 or > 14 || password.Length is < 4 or > 14)
            return new AuthResult(AuthStatus.InvalidCredentials, ErrorMessage: "Invalid credentials");

        // Credential check
        var user = await _userRepo.GetByUsernameAsync(username, ct);
        if (user is null || user.Password != password)
            return new AuthResult(AuthStatus.InvalidCredentials, ErrorMessage: "Invalid credentials");

        if (user.IsBanned)
            return new AuthResult(AuthStatus.Banned, ErrorMessage: "Account is banned");

        // Duplicate login check
        if (IsOnline(user.Id))
            return new AuthResult(AuthStatus.AlreadyOnline, ErrorMessage: "Already logged in");

        _logger.LogInformation("Auth success for {Username}", username);
        return new AuthResult(AuthStatus.Success, User: user);
    }

    public async Task<IReadOnlyList<Character>> GetCharacterListAsync(int userId, CancellationToken ct = default)
    {
        return await _characterRepo.GetByUserIdAsync(userId, ct);
    }

    public async Task<Character?> SelectCharacterAsync(int userId, byte slot, CancellationToken ct = default)
    {
        var characters = await _characterRepo.GetByUserIdAsync(userId, ct);
        return characters.FirstOrDefault(c => c.Slot == slot);
    }

    public bool IsOnline(int userId)
    {
        return _players.GetAll().Any(p => p.UserId == userId);
    }
}
