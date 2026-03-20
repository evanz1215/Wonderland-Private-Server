using FluentAssertions;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;
using Xunit;

namespace Wonderland.Domain.Tests.Entities;

public class CharacterTests
{
    [Fact]
    public void NewCharacter_ShouldHaveDefaultValues()
    {
        var character = new Character();

        character.Level.Should().Be(1);
        character.Gold.Should().Be(0);
        character.TotalExp.Should().Be(0);
        character.RebornJob.Should().Be(RebornJob.None);
        character.IsReborn.Should().BeFalse();
        character.Inventory.Should().BeEmpty();
        character.Pets.Should().BeEmpty();
    }

    [Fact]
    public void Character_ShouldAcceptValidValues()
    {
        var character = new Character
        {
            CharId = 1,
            UserId = 100,
            Name = "TestPlayer",
            Level = 50,
            Gold = 10000,
            Str = 20,
            Con = 15,
            Agi = 25,
            Int = 10,
            Wis = 30,
            MapId = 60000,
            X = 100,
            Y = 200,
            Affinity = Affinity.Fire,
        };

        character.CharId.Should().Be(1);
        character.Name.Should().Be("TestPlayer");
        character.Level.Should().Be(50);
        character.Affinity.Should().Be(Affinity.Fire);
    }
}
