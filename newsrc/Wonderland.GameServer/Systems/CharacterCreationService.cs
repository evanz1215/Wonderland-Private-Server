using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;
using Wonderland.Domain.Interfaces;

namespace Wonderland.GameServer.Systems;

/// <summary>
/// Handles character creation: name validation, cipher password, and new character persistence.
/// </summary>
public partial class CharacterCreationService : ICharacterCreationService
{
    private const int MaxCharactersPerAccount = 3;
    private const int MinNameLength = 4;
    private const int MaxNameLength = 14;
    private const int MinCipherLength = 6;
    private const int MaxCipherLength = 14;

    // Starting location: Ship map
    private const int StartMapId = 60000;
    private const ushort StartX = 602;
    private const ushort StartY = 455;
    private const long StartExp = 6;

    // Base HP/SP for a new level 1 character
    private const int BaseHp = 50;
    private const int BaseSp = 30;

    private static readonly HashSet<string> RestrictedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "administrator", "gm", "gamemaster", "moderator", "mod",
        "system", "server", "wonderland", "wlo", "null", "undefined",
    };

    private readonly ICharacterRepository _characterRepo;
    private readonly IUserRepository _userRepo;
    private readonly ILogger<CharacterCreationService> _logger;

    public CharacterCreationService(
        ICharacterRepository characterRepo,
        IUserRepository userRepo,
        ILogger<CharacterCreationService> logger)
    {
        _characterRepo = characterRepo;
        _userRepo = userRepo;
        _logger = logger;
    }

    public async Task<NameValidationResult> ValidateAndReserveNameAsync(int userId, string name, CancellationToken ct = default)
    {
        // Length check
        if (name.Length < MinNameLength)
            return NameValidationResult.TooShort;
        if (name.Length > MaxNameLength)
            return NameValidationResult.TooLong;

        // Character validation: alphanumeric and common CJK characters
        if (!ValidNameRegex().IsMatch(name))
            return NameValidationResult.Invalid;

        // Restricted names
        if (RestrictedNames.Contains(name))
            return NameValidationResult.Restricted;

        // Uniqueness check (case-insensitive via PostgreSQL ILike)
        if (await _characterRepo.IsNameTakenAsync(name, ct))
            return NameValidationResult.Taken;

        return NameValidationResult.Available;
    }

    public async Task<bool> SetCipherPasswordAsync(int userId, string cipher, CancellationToken ct = default)
    {
        if (cipher.Length is < MinCipherLength or > MaxCipherLength)
            return false;

        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null)
            return false;

        // Only allow setting cipher once
        if (!string.IsNullOrEmpty(user.CipherPassword))
            return false;

        user.CipherPassword = cipher;
        await _userRepo.UpdateAsync(user, ct);
        await _userRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Cipher password set for user {UserId}", userId);
        return true;
    }

    public async Task<CharacterCreationResult> CreateCharacterAsync(CharacterCreationRequest req, CancellationToken ct = default)
    {
        // Validate slot availability
        var existingCount = await _characterRepo.GetCharacterCountAsync(req.UserId, ct);
        if (existingCount >= MaxCharactersPerAccount)
            return new CharacterCreationResult(CharacterCreationStatus.SlotFull);

        // Validate name one more time (race condition guard)
        if (string.IsNullOrEmpty(req.Name))
            return new CharacterCreationResult(CharacterCreationStatus.NameInvalid);

        if (await _characterRepo.IsNameTakenAsync(req.Name, ct))
            return new CharacterCreationResult(CharacterCreationStatus.NameTaken);

        // Calculate initial HP/SP based on stats
        var initialHp = BaseHp + req.Con * 4;
        var initialSp = BaseSp + req.Wis * 3;

        var character = new Character
        {
            UserId = req.UserId,
            Slot = req.Slot,
            Name = req.Name,
            Body = req.Body,
            Head = req.Head,
            Hair = req.HairColor,
            Skin = req.SkinColor,
            Clothing = req.ClothingColor,
            Eyes = req.EyeColor,
            Affinity = req.Element,
            Str = req.Str,
            Agi = req.Agi,
            Wis = req.Wis,
            Int = req.Int,
            Con = req.Con,
            Level = 1,
            TotalExp = StartExp,
            Gold = 0,
            MaxHp = initialHp,
            MaxSp = initialSp,
            CurrentHp = initialHp,
            CurrentSp = initialSp,
            MapId = StartMapId,
            X = StartX,
            Y = StartY,
        };

        try
        {
            await _characterRepo.AddAsync(character, ct);
            await _characterRepo.SaveChangesAsync(ct);

            _logger.LogInformation("Character created: {Name} (ID: {CharId}) for user {UserId}",
                character.Name, character.CharId, req.UserId);

            return new CharacterCreationResult(CharacterCreationStatus.Success, character);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create character {Name} for user {UserId}", req.Name, req.UserId);
            return new CharacterCreationResult(CharacterCreationStatus.Failed, ErrorMessage: ex.Message);
        }
    }

    public async Task<byte?> GetNextAvailableSlotAsync(int userId, CancellationToken ct = default)
    {
        var characters = await _characterRepo.GetByUserIdAsync(userId, ct);
        var usedSlots = characters.Select(c => c.Slot).ToHashSet();

        for (byte slot = 0; slot < MaxCharactersPerAccount; slot++)
        {
            if (!usedSlots.Contains(slot))
                return slot;
        }

        return null;
    }

    /// <summary>
    /// Validates character names: allows alphanumeric, CJK unified ideographs, and common punctuation.
    /// </summary>
    [GeneratedRegex(@"^[\w\u4e00-\u9fff\u3400-\u4dbf]+$", RegexOptions.Compiled)]
    private static partial Regex ValidNameRegex();
}
