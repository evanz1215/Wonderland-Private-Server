using FluentAssertions;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;
using Wonderland.GameServer.Systems;
using Xunit;

namespace Wonderland.Application.Tests.Services;

public class PlayerRuntimeManagerTests
{
    private readonly PlayerRuntimeManager _manager = new();

    private static PlayerRuntime CreatePlayer(int charId = 1, string name = "Player1", ushort mapId = 60000) => new()
    {
        UserId = 100,
        CharId = charId,
        SessionId = Guid.NewGuid().ToString("N")[..12],
        CharName = name,
        MapId = mapId,
        Level = 10,
    };

    [Fact]
    public void Add_ShouldIncrementOnlineCount()
    {
        _manager.Add(CreatePlayer());
        _manager.OnlineCount.Should().Be(1);
    }

    [Fact]
    public void Remove_ShouldDecrementOnlineCount()
    {
        var player = CreatePlayer();
        _manager.Add(player);
        _manager.Remove(player.CharId);
        _manager.OnlineCount.Should().Be(0);
    }

    [Fact]
    public void GetByCharId_ShouldReturnCorrectPlayer()
    {
        var player = CreatePlayer(charId: 42, name: "Hero");
        _manager.Add(player);

        var found = _manager.GetByCharId(42);
        found.Should().NotBeNull();
        found!.CharName.Should().Be("Hero");
    }

    [Fact]
    public void GetByCharName_ShouldBeCaseInsensitive()
    {
        var player = CreatePlayer(charId: 7, name: "Wizard");
        _manager.Add(player);

        _manager.GetByCharName("wizard").Should().NotBeNull();
        _manager.GetByCharName("WIZARD").Should().NotBeNull();
    }

    [Fact]
    public void GetBySessionId_ShouldReturnCorrectPlayer()
    {
        var player = CreatePlayer();
        _manager.Add(player);

        _manager.GetBySessionId(player.SessionId).Should().NotBeNull();
    }

    [Fact]
    public void GetByCharId_ShouldReturnNull_WhenNotFound()
    {
        _manager.GetByCharId(999).Should().BeNull();
    }

    [Fact]
    public void GetAll_ShouldReturnAllPlayers()
    {
        _manager.Add(CreatePlayer(1, "A"));
        _manager.Add(CreatePlayer(2, "B"));
        _manager.Add(CreatePlayer(3, "C"));

        _manager.GetAll().Count.Should().Be(3);
    }

    [Fact]
    public void GetByMapId_ShouldFilterByMap()
    {
        _manager.Add(CreatePlayer(1, "A", mapId: 60000));
        _manager.Add(CreatePlayer(2, "B", mapId: 60000));
        _manager.Add(CreatePlayer(3, "C", mapId: 10019));

        _manager.GetByMapId(60000).Count.Should().Be(2);
        _manager.GetByMapId(10019).Count.Should().Be(1);
        _manager.GetByMapId(55555).Count.Should().Be(0);
    }

    [Fact]
    public void Remove_ShouldCleanUpAllIndexes()
    {
        var player = CreatePlayer(42, "TestPlayer");
        _manager.Add(player);
        _manager.Remove(42);

        _manager.GetByCharId(42).Should().BeNull();
        _manager.GetByCharName("TestPlayer").Should().BeNull();
        _manager.GetBySessionId(player.SessionId).Should().BeNull();
    }
}
