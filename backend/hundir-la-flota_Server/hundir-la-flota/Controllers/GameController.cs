using hundir_la_flota.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/game")]
[Authorize] // Solo usuarios autenticados pueden acceder a estos endpoints
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
    }


    [HttpPost("create")]
    public async Task<IActionResult> CreateGame()
    {
        var userId = User.FindFirst("id")?.Value;
        var result = await _gameService.CreateGameAsync(userId);

        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return Ok(result.Data);
    }

    [HttpPost("{gameId}/join")]
    public IActionResult JoinGame(Guid gameId, int playerId)
    {
        var result = _gameService.JoinGameAsync(gameId, playerId).Result;

        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return Ok(result.Message);
    }


    [HttpPost("{gameId}/place-ships")]
    public IActionResult PlaceShips(Guid gameId, int playerId, List<Ship> ships)
    {
        var result = _gameService.PlaceShipsAsync(gameId, playerId, ships).Result;

        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return Ok(result.Message);
    }



    [HttpPost("{gameId}/attack")]
    public IActionResult Attack(Guid gameId, int playerId, int x, int y)
    {
        var result = _gameService.AttackAsync(gameId, playerId, x, y).Result;

        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return Ok(result.Message);
    }


    [HttpGet("{gameId}")]
    public async Task<IActionResult> GetGameState(Guid gameId)
    {
        var userId = User.FindFirst("id")?.Value;
        var result = await _gameService.GetGameStateAsync(userId, gameId);

        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return Ok(result.Data);
    }
}
