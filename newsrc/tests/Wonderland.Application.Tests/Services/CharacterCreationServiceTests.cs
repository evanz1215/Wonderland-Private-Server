using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;
using Wonderland.Domain.Interfaces;
using Wonderland.GameServer.Systems;
using Xunit;

namespace Wonderland.Application.Tests.Services;

public class CharacterCreationServiceTests
{
    private readonly ICharacterRepository _charRepo = Substitute.For<ICharacterRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ILogger<CharacterCreationService> _logger = Substitute.For<ILogger<CharacterCreationService>>();
    private readonly CharacterCreationService _service;

    public CharacterCreationServiceTests()
    {
        _service = new CharacterCreationService(_charRepo, _userRepo, _logger);
    }

    #region ValidateAndReserveNameAsync

    [Fact]
    public async Task ValidateName_ShouldReturnTooShort_WhenNameTooShort()
    {
        var result = await _service.ValidateAndReserveNameAsync(1, "abc");
        result.Should().Be(NameValidationResult.TooShort);
    }

    [Fact]
    public async Task ValidateName_ShouldReturnTooLong_WhenNameTooLong()
    {
        var result = await _service.ValidateAndReserveNameAsync(1, "abcdefghijklmno"); // 15 chars
        result.Should().Be(NameValidationResult.TooLong);
    }

    [Fact]
    public async Task ValidateName_ShouldReturnInvalid_WhenNameContainsSpecialChars()
    {
        var result = await _service.ValidateAndReserveNameAsync(1, "test@name");
        result.Should().Be(NameValidationResult.Invalid);
    }

    [Fact]
    public async Task ValidateName_ShouldReturnRestricted_WhenNameIsRestricted()
    {
        var result = await _service.ValidateAndReserveNameAsync(1, "admin");
        result.Should().Be(NameValidationResult.Restricted);
    }

    [Fact]
    public async Task ValidateName_ShouldReturnRestricted_CaseInsensitive()
    {
        var result = await _service.ValidateAndReserveNameAsync(1, "ADMIN");
        result.Should().Be(NameValidationResult.Restricted);
    }

    [Fact]
    public async Task ValidateName_ShouldReturnTaken_WhenNameExists()
    {
        _charRepo.IsNameTakenAsync("TestHero", Arg.Any<CancellationToken>()).Returns(true);
        var result = await _service.ValidateAndReserveNameAsync(1, "TestHero");
        result.Should().Be(NameValidationResult.Taken);
    }

    [Fact]
    public async Task ValidateName_ShouldReturnAvailable_WhenValid()
    {
        _charRepo.IsNameTakenAsync("ValidHero", Arg.Any<CancellationToken>()).Returns(false);
        var result = await _service.ValidateAndReserveNameAsync(1, "ValidHero");
        result.Should().Be(NameValidationResult.Available);
    }

    [Fact]
    public async Task ValidateName_ShouldAcceptCjkCharacters()
    {
        _charRepo.IsNameTakenAsync("勇者測試", Arg.Any<CancellationToken>()).Returns(false);
        var result = await _service.ValidateAndReserveNameAsync(1, "勇者測試");
        result.Should().Be(NameValidationResult.Available);
    }

    [Fact]
    public async Task ValidateName_ShouldReturnAvailable_WhenExactly4Chars()
    {
        _charRepo.IsNameTakenAsync("abcd", Arg.Any<CancellationToken>()).Returns(false);
        var result = await _service.ValidateAndReserveNameAsync(1, "abcd");
        result.Should().Be(NameValidationResult.Available);
    }

    [Fact]
    public async Task ValidateName_ShouldReturnAvailable_WhenExactly14Chars()
    {
        var name = "abcdefghijklmn"; // 14 chars
        _charRepo.IsNameTakenAsync(name, Arg.Any<CancellationToken>()).Returns(false);
        var result = await _service.ValidateAndReserveNameAsync(1, name);
        result.Should().Be(NameValidationResult.Available);
    }

    [Theory]
    [InlineData("gamemaster")]
    [InlineData("moderator")]
    [InlineData("system")]
    [InlineData("server")]
    [InlineData("wonderland")]
    public async Task ValidateName_ShouldRejectAllRestrictedNames(string name)
    {
        var result = await _service.ValidateAndReserveNameAsync(1, name);
        result.Should().Be(NameValidationResult.Restricted);
    }

    #endregion

    #region SetCipherPasswordAsync

    [Fact]
    public async Task SetCipher_ShouldReturnFalse_WhenTooShort()
    {
        var result = await _service.SetCipherPasswordAsync(1, "12345");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetCipher_ShouldReturnFalse_WhenTooLong()
    {
        var result = await _service.SetCipherPasswordAsync(1, "123456789012345"); // 15 chars
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetCipher_ShouldReturnFalse_WhenUserNotFound()
    {
        _userRepo.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((User?)null);
        var result = await _service.SetCipherPasswordAsync(999, "password");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetCipher_ShouldReturnFalse_WhenAlreadySet()
    {
        var user = new User { Id = 1, CipherPassword = "existing" };
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        var result = await _service.SetCipherPasswordAsync(1, "newpass");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetCipher_ShouldReturnTrue_AndPersist()
    {
        var user = new User { Id = 1, CipherPassword = "" };
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _service.SetCipherPasswordAsync(1, "secret");
        result.Should().BeTrue();
        user.CipherPassword.Should().Be("secret");
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _userRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region CreateCharacterAsync

    private static CharacterCreationRequest MakeRequest(int userId = 1, byte slot = 0, string name = "Hero") => new(
        UserId: userId,
        Slot: slot,
        Name: name,
        Body: 1,
        Head: 2,
        HairColor: 3,
        SkinColor: 4,
        ClothingColor: 5,
        EyeColor: 6,
        Element: Affinity.Fire,
        Str: 10,
        Agi: 8,
        Wis: 6,
        Int: 4,
        Con: 12
    );

    [Fact]
    public async Task CreateCharacter_ShouldReturnSlotFull_WhenMaxReached()
    {
        _charRepo.GetCharacterCountAsync(1, Arg.Any<CancellationToken>()).Returns(3);
        var result = await _service.CreateCharacterAsync(MakeRequest());
        result.Status.Should().Be(CharacterCreationStatus.SlotFull);
    }

    [Fact]
    public async Task CreateCharacter_ShouldReturnNameInvalid_WhenEmpty()
    {
        _charRepo.GetCharacterCountAsync(1, Arg.Any<CancellationToken>()).Returns(0);
        var result = await _service.CreateCharacterAsync(MakeRequest(name: ""));
        result.Status.Should().Be(CharacterCreationStatus.NameInvalid);
    }

    [Fact]
    public async Task CreateCharacter_ShouldReturnNameTaken_OnRaceCondition()
    {
        _charRepo.GetCharacterCountAsync(1, Arg.Any<CancellationToken>()).Returns(0);
        _charRepo.IsNameTakenAsync("Hero", Arg.Any<CancellationToken>()).Returns(true);
        var result = await _service.CreateCharacterAsync(MakeRequest());
        result.Status.Should().Be(CharacterCreationStatus.NameTaken);
    }

    [Fact]
    public async Task CreateCharacter_ShouldReturnSuccess_WithCorrectFields()
    {
        _charRepo.GetCharacterCountAsync(1, Arg.Any<CancellationToken>()).Returns(0);
        _charRepo.IsNameTakenAsync("Hero", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.CreateCharacterAsync(MakeRequest());

        result.Status.Should().Be(CharacterCreationStatus.Success);
        result.Character.Should().NotBeNull();

        var c = result.Character!;
        c.Name.Should().Be("Hero");
        c.UserId.Should().Be(1);
        c.Slot.Should().Be(0);
        c.Body.Should().Be(1);
        c.Head.Should().Be(2);
        c.Hair.Should().Be(3);
        c.Skin.Should().Be(4);
        c.Clothing.Should().Be(5);
        c.Eyes.Should().Be(6);
        c.Affinity.Should().Be(Affinity.Fire);
        c.Str.Should().Be(10);
        c.Agi.Should().Be(8);
        c.Wis.Should().Be(6);
        c.Int.Should().Be(4);
        c.Con.Should().Be(12);
        c.Level.Should().Be(1);
        c.TotalExp.Should().Be(6);
        c.Gold.Should().Be(0);
        c.MapId.Should().Be(60000);
        c.X.Should().Be(602);
        c.Y.Should().Be(455);
        c.MaxHp.Should().Be(50 + 12 * 4); // BaseHp + Con * 4
        c.MaxSp.Should().Be(30 + 6 * 3);  // BaseSp + Wis * 3
        c.CurrentHp.Should().Be(c.MaxHp);
        c.CurrentSp.Should().Be(c.MaxSp);

        await _charRepo.Received(1).AddAsync(c, Arg.Any<CancellationToken>());
        await _charRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCharacter_ShouldReturnFailed_OnDbException()
    {
        _charRepo.GetCharacterCountAsync(1, Arg.Any<CancellationToken>()).Returns(0);
        _charRepo.IsNameTakenAsync("Hero", Arg.Any<CancellationToken>()).Returns(false);
        _charRepo.When(r => r.AddAsync(Arg.Any<Character>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("DB error"));

        var result = await _service.CreateCharacterAsync(MakeRequest());
        result.Status.Should().Be(CharacterCreationStatus.Failed);
        result.ErrorMessage.Should().Contain("DB error");
    }

    #endregion

    #region GetNextAvailableSlotAsync

    [Fact]
    public async Task GetNextSlot_ShouldReturn0_WhenNoCharacters()
    {
        _charRepo.GetByUserIdAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Character>());
        var slot = await _service.GetNextAvailableSlotAsync(1);
        slot.Should().Be(0);
    }

    [Fact]
    public async Task GetNextSlot_ShouldReturn1_WhenSlot0Taken()
    {
        _charRepo.GetByUserIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<Character> { new() { Slot = 0 } });
        var slot = await _service.GetNextAvailableSlotAsync(1);
        slot.Should().Be(1);
    }

    [Fact]
    public async Task GetNextSlot_ShouldReturnNull_WhenAllSlotsFull()
    {
        _charRepo.GetByUserIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<Character> { new() { Slot = 0 }, new() { Slot = 1 }, new() { Slot = 2 } });
        var slot = await _service.GetNextAvailableSlotAsync(1);
        slot.Should().BeNull();
    }

    [Fact]
    public async Task GetNextSlot_ShouldFindGap_WhenMiddleSlotFree()
    {
        _charRepo.GetByUserIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<Character> { new() { Slot = 0 }, new() { Slot = 2 } });
        var slot = await _service.GetNextAvailableSlotAsync(1);
        slot.Should().Be(1);
    }

    #endregion
}
