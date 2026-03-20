using FluentAssertions;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;
using Xunit;

namespace Wonderland.Domain.Tests.Entities;

public class PlayerRuntimeTests
{
    [Fact]
    public void FromCharacter_ShouldMapAllFields()
    {
        var user = new User
        {
            Id = 100,
            Username = "test",
            Password = "pass",
            CipherPassword = "cipher123",
            GmLevel = GmStatus.Admin,
            ImPoints = 500,
        };

        var character = new Character
        {
            CharId = 42,
            UserId = 100,
            Name = "Hero",
            Level = 99,
            Gold = 50000,
            MapId = 60000,
            X = 100,
            Y = 200,
            Str = 50,
            Con = 40,
            Agi = 30,
            Int = 20,
            Wis = 10,
            Affinity = Affinity.Fire,
            RebornJob = RebornJob.Knight,
            IsReborn = true,
        };

        var runtime = PlayerRuntime.FromCharacter(character, user, "session123");

        runtime.UserId.Should().Be(100);
        runtime.CharId.Should().Be(42);
        runtime.SessionId.Should().Be("session123");
        runtime.CharName.Should().Be("Hero");
        runtime.GmLevel.Should().Be(GmStatus.Admin);
        runtime.ImPoints.Should().Be(500);
        runtime.Level.Should().Be(99);
        runtime.Gold.Should().Be(50000);
        runtime.MapId.Should().Be(60000);
        runtime.X.Should().Be(100);
        runtime.Y.Should().Be(200);
        runtime.Affinity.Should().Be(Affinity.Fire);
        runtime.RebornJob.Should().Be(RebornJob.Knight);
        runtime.IsReborn.Should().BeTrue();
    }

    [Fact]
    public void SyncToCharacter_ShouldWriteBackChanges()
    {
        var character = new Character { CharId = 1, Name = "Test" };
        var user = new User { Id = 1 };
        var runtime = PlayerRuntime.FromCharacter(character, user, "sess");

        runtime.Level = 50;
        runtime.Gold = 99999;
        runtime.MapId = 10019;
        runtime.X = 300;
        runtime.Y = 400;

        runtime.SyncToCharacter(character);

        character.Level.Should().Be(50);
        character.Gold.Should().Be(99999);
        character.MapId.Should().Be(10019);
        character.X.Should().Be(300);
        character.Y.Should().Be(400);
    }

    [Fact]
    public void Flags_ShouldBeThreadSafe()
    {
        var user = new User { Id = 1 };
        var character = new Character { CharId = 1, Name = "Test" };
        var runtime = PlayerRuntime.FromCharacter(character, user, "sess");

        runtime.HasFlag(PlayerFlag.InGame).Should().BeFalse();

        runtime.AddFlag(PlayerFlag.InGame);
        runtime.HasFlag(PlayerFlag.InGame).Should().BeTrue();

        runtime.RemoveFlag(PlayerFlag.InGame);
        runtime.HasFlag(PlayerFlag.InGame).Should().BeFalse();
    }

    [Fact]
    public void IsMuted_ShouldReturnTrue_WhenMuteNotExpired()
    {
        var user = new User { Id = 1 };
        var character = new Character { CharId = 1, Name = "Test" };
        var runtime = PlayerRuntime.FromCharacter(character, user, "sess");

        runtime.MuteUntil = DateTime.UtcNow.AddMinutes(10);
        runtime.IsMuted.Should().BeTrue();
    }

    [Fact]
    public void IsMuted_ShouldReturnFalse_WhenMuteExpired()
    {
        var user = new User { Id = 1 };
        var character = new Character { CharId = 1, Name = "Test" };
        var runtime = PlayerRuntime.FromCharacter(character, user, "sess");

        runtime.MuteUntil = DateTime.UtcNow.AddMinutes(-1);
        runtime.IsMuted.Should().BeFalse();
    }

    [Fact]
    public void IsInBattle_ShouldDependOnBattleId()
    {
        var user = new User { Id = 1 };
        var character = new Character { CharId = 1, Name = "Test" };
        var runtime = PlayerRuntime.FromCharacter(character, user, "sess");

        runtime.IsInBattle.Should().BeFalse();
        runtime.BattleId = 42;
        runtime.IsInBattle.Should().BeTrue();
        runtime.BattleId = null;
        runtime.IsInBattle.Should().BeFalse();
    }
}
