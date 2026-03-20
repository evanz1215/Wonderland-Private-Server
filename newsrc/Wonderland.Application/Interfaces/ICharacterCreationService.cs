using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;

namespace Wonderland.Application.Interfaces;

/// <summary>
/// Handles character creation flow: name validation, cipher password, and character persistence.
/// </summary>
public interface ICharacterCreationService
{
    /// <summary>Validate and reserve a character name for the given user.</summary>
    Task<NameValidationResult> ValidateAndReserveNameAsync(int userId, string name, CancellationToken ct = default);

    /// <summary>Set the cipher (secondary) password for a user account. Only allowed once.</summary>
    Task<bool> SetCipherPasswordAsync(int userId, string cipher, CancellationToken ct = default);

    /// <summary>Create a new character with the given appearance and stats.</summary>
    Task<CharacterCreationResult> CreateCharacterAsync(CharacterCreationRequest request, CancellationToken ct = default);

    /// <summary>Get the next available slot for a user (0-2).</summary>
    Task<byte?> GetNextAvailableSlotAsync(int userId, CancellationToken ct = default);
}

public record CharacterCreationRequest(
    int UserId,
    byte Slot,
    string Name,
    ushort Body,
    ushort Head,
    ushort HairColor,
    ushort SkinColor,
    ushort ClothingColor,
    ushort EyeColor,
    Affinity Element,
    byte Str,
    byte Agi,
    byte Wis,
    byte Int,
    byte Con
);

public record CharacterCreationResult(
    CharacterCreationStatus Status,
    Character? Character = null,
    string? ErrorMessage = null
);

public enum CharacterCreationStatus
{
    Success,
    NameTaken,
    NameInvalid,
    SlotFull,
    CipherRequired,
    Failed,
}

public enum NameValidationResult
{
    Available,
    Taken,
    Invalid,
    TooShort,
    TooLong,
    Restricted,
}
