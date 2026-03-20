using Microsoft.AspNetCore.Mvc;
using Wonderland.Application.Interfaces;

namespace Wonderland.WebApi.Controllers;

/// <summary>
/// REST API for server management — start/stop/status
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ServerController : ControllerBase
{
    private readonly IGameServerManager _serverManager;
    private readonly IWorldEventService _eventService;

    public ServerController(IGameServerManager serverManager, IWorldEventService eventService)
    {
        _serverManager = serverManager;
        _eventService = eventService;
    }

    [HttpGet("status")]
    public ActionResult<ServerStatus> GetStatus()
        => Ok(_serverManager.GetStatus());

    [HttpPost("start")]
    public async Task<IActionResult> Start(CancellationToken ct)
    {
        if (_serverManager.IsRunning)
            return BadRequest(new { Message = "Server is already running" });

        await _serverManager.StartAsync(ct);
        return Ok(new { Message = "Server started" });
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop(CancellationToken ct)
    {
        if (!_serverManager.IsRunning)
            return BadRequest(new { Message = "Server is not running" });

        await _serverManager.StopAsync(ct);
        return Ok(new { Message = "Server stopped" });
    }

    [HttpPost("events/double-exp")]
    public IActionResult StartDoubleExp([FromQuery] int minutes = 60)
    {
        _eventService.StartDoubleExp(TimeSpan.FromMinutes(minutes));
        return Ok(new { Message = $"Double EXP started for {minutes} minutes" });
    }

    [HttpPost("events/double-drop")]
    public IActionResult StartDoubleDrop([FromQuery] int minutes = 60)
    {
        _eventService.StartDoubleDrop(TimeSpan.FromMinutes(minutes));
        return Ok(new { Message = $"Double drop started for {minutes} minutes" });
    }

    [HttpGet("events")]
    public IActionResult GetActiveEvents()
        => Ok(_eventService.GetActiveEvents());
}
