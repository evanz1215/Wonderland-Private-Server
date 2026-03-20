using Microsoft.AspNetCore.Mvc;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Interfaces;
using Wonderland.WebApi.Hubs;

namespace Wonderland.WebApi.Controllers;

/// <summary>
/// REST API for player management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly IPlayerRuntimeManager _runtimeManager;
    private readonly ICharacterRepository _characterRepo;

    public PlayersController(IPlayerRuntimeManager runtimeManager, ICharacterRepository characterRepo)
    {
        _runtimeManager = runtimeManager;
        _characterRepo = characterRepo;
    }

    [HttpGet("online")]
    public ActionResult<IReadOnlyCollection<PlayerRuntimeDto>> GetOnlinePlayers()
        => Ok(_runtimeManager.GetAll().Select(p => new PlayerRuntimeDto(
            p.CharId, p.CharName, p.Level, p.MapId, p.X, p.Y, p.IsInBattle)));

    [HttpGet("online/count")]
    public ActionResult<int> GetOnlineCount()
        => Ok(_runtimeManager.OnlineCount);

    [HttpGet("{charId:int}")]
    public async Task<IActionResult> GetCharacter(int charId, CancellationToken ct)
    {
        var character = await _characterRepo.GetByIdAsync(charId, ct);
        if (character is null) return NotFound();
        return Ok(character);
    }

    [HttpGet("{charId:int}/inventory")]
    public async Task<IActionResult> GetInventory(int charId, CancellationToken ct)
    {
        var character = await _characterRepo.GetWithInventoryAsync(charId, ct);
        if (character is null) return NotFound();
        return Ok(character.Inventory);
    }

    [HttpGet("{charId:int}/pets")]
    public async Task<IActionResult> GetPets(int charId, CancellationToken ct)
    {
        var character = await _characterRepo.GetWithPetsAsync(charId, ct);
        if (character is null) return NotFound();
        return Ok(character.Pets);
    }
}
