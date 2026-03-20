using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Wonderland.Application.Interfaces;
using Wonderland.WebApi.Controllers;
using Xunit;

namespace Wonderland.WebApi.Tests.Controllers;

public class ServerControllerTests
{
    private readonly IGameServerManager _serverManager = Substitute.For<IGameServerManager>();
    private readonly IWorldEventService _eventService = Substitute.For<IWorldEventService>();
    private readonly ServerController _controller;

    public ServerControllerTests()
    {
        _controller = new ServerController(_serverManager, _eventService);
    }

    [Fact]
    public void GetStatus_ShouldReturnServerStatus()
    {
        var expected = new ServerStatus(true, 10, DateTime.UtcNow, TimeSpan.FromHours(1), 6414, 1.0, 1.0);
        _serverManager.GetStatus().Returns(expected);

        var result = _controller.GetStatus();
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Start_ShouldReturnBadRequest_WhenAlreadyRunning()
    {
        _serverManager.IsRunning.Returns(true);
        var result = await _controller.Start(CancellationToken.None);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Start_ShouldReturnOk_WhenNotRunning()
    {
        _serverManager.IsRunning.Returns(false);
        var result = await _controller.Start(CancellationToken.None);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Stop_ShouldReturnBadRequest_WhenNotRunning()
    {
        _serverManager.IsRunning.Returns(false);
        var result = await _controller.Stop(CancellationToken.None);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void StartDoubleExp_ShouldCallEventService()
    {
        var result = _controller.StartDoubleExp(30);
        result.Should().BeOfType<OkObjectResult>();
        _eventService.Received(1).StartDoubleExp(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void StartDoubleDrop_ShouldCallEventService()
    {
        var result = _controller.StartDoubleDrop(60);
        result.Should().BeOfType<OkObjectResult>();
        _eventService.Received(1).StartDoubleDrop(TimeSpan.FromMinutes(60));
    }

    [Fact]
    public void GetActiveEvents_ShouldReturnOk()
    {
        _eventService.GetActiveEvents().Returns([]);
        var result = _controller.GetActiveEvents();
        result.Should().BeOfType<OkObjectResult>();
    }
}
