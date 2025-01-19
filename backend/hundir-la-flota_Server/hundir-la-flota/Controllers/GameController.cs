using hundir_la_flota.Models;
using hundir_la_flota.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/game")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly IWebSocketService _webSocketService;

    public GameController(IGameService gameService, IWebSocketService webSocketService)
    {
        _gameService = gameService;
        _webSocketService = webSocketService;
    }

    private int GetUserIdFromClaim()
    {
        var userIdClaim = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new InvalidOperationException("No se encontró el ID del usuario en el token.");
        return int.Parse(userIdClaim);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateGame()
    {
        var userId = GetUserIdFromClaim();
        var response = await _gameService.CreateGameAsync(userId.ToString());
        if (!response.Success)
            return BadRequest(response.Message);
        return Ok(response.Data);
    }

    [HttpPost("{gameId}/join")]
    public async Task<IActionResult> JoinGame(Guid gameId, [FromBody] int playerId)
    {
        var response = await _gameService.JoinGameAsync(gameId, playerId);
        if (!response.Success)
            return BadRequest(response.Message);
        return Ok(response.Message);
    }

    [HttpPost("{gameId}/place-ships")]
    public async Task<IActionResult> PlaceShips(Guid gameId, [FromBody] PlaceShipsRequest request)
    {
        var response = await _gameService.PlaceShipsAsync(gameId, request.PlayerId, request.Ships);
        if (!response.Success)
            return BadRequest(response.Message);
        return Ok(response.Message);
    }

    [HttpPost("{gameId}/attack")]
    public async Task<IActionResult> Attack(Guid gameId, [FromBody] AttackRequest request)
    {
        var response = await _gameService.AttackAsync(gameId, request.PlayerId, request.X, request.Y);
        if (!response.Success)
            return BadRequest(response.Message);
        return Ok(response.Message);
    }

    [HttpGet("{gameId}")]
    public async Task<IActionResult> GetGameState(Guid gameId)
    {
        var userId = GetUserIdFromClaim();
        var response = await _gameService.GetGameStateAsync(userId.ToString(), gameId);
        if (!response.Success)
            return BadRequest(response.Message);
        return Ok(response.Data);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteFriend([FromBody] int friendId)
    {
        var currentUserId = GetUserIdFromClaim();
        await _webSocketService.NotifyUserAsync(friendId, "GameInvitation", currentUserId.ToString());
        return Ok("Invitación enviada.");
    }

    [HttpPost("accept-invitation")]
    public async Task<IActionResult> AcceptInvitation([FromBody] int hostId)
    {
        var currentUserId = GetUserIdFromClaim();
        await _webSocketService.NotifyUserAsync(hostId, "InvitationAccepted", currentUserId.ToString());
        return Ok("Invitación aceptada.");
    }

    [HttpPost("join-random-match")]
    public async Task<IActionResult> JoinRandomMatch()
    {
        var userId = GetUserIdFromClaim();
        var response = await _gameService.FindRandomOpponentAsync(userId.ToString());
        if (!response.Success)
            return Ok("Esperando a un oponente...");
        return Ok(new { OpponentId = response.Data.GameId });
    }

    [HttpPost("play-with-bot")]
    public async Task<IActionResult> PlayWithBot()
    {
        var userId = GetUserIdFromClaim();
        var response = await _gameService.CreateBotGameAsync(userId.ToString());
        if (!response.Success)
            return BadRequest(response.Message);
        return Ok(response.Data);
    }
}

// Clases auxiliares
public class PlaceShipsRequest
{
    public int PlayerId { get; set; }
    public List<Ship> Ships { get; set; }
}

public class AttackRequest
{
    public int PlayerId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}
