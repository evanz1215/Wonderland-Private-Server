using Wonderland.Domain.Entities;

namespace Wonderland.Application.Interfaces;

/// <summary>
/// Handles player authentication and session lifecycle.
/// </summary>
public interface IAuthService
{
    /// <summary>Validate login credentials. Returns user on success, null on failure.</summary>
    Task<AuthResult> LoginAsync(string username, string password, ushort clientVersion, CancellationToken ct = default);

    /// <summary>Load character list for a user</summary>
    Task<IReadOnlyList<Character>> GetCharacterListAsync(int userId, CancellationToken ct = default);

    /// <summary>Select and load a character for play</summary>
    Task<Character?> SelectCharacterAsync(int userId, byte slot, CancellationToken ct = default);

    /// <summary>Check if a user is already online</summary>
    bool IsOnline(int userId);
}

public record AuthResult(
    AuthStatus Status,
    User? User = null,
    string? ErrorMessage = null
);

public enum AuthStatus
{
    Success,
    InvalidCredentials,
    AlreadyOnline,
    VersionMismatch,
    Banned,
}
