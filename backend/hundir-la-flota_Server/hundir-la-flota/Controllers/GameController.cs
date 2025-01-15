using hundir_la_flota.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

[ApiController]
[Route("api/game")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
    }

    private async Task NotifyUserViaWebSocket(string userId, string action, string payload)
    {
        if (WebSocketController.ConnectedUsers.TryGetValue(userId, out var webSocket))
        {
            var message = $"{action}|{payload}";
            var bytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
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

    [HttpPost("invite")]
    public async Task<IActionResult> InviteFriend(string friendId)
    {
        var userId = User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(friendId))
        {
            return BadRequest("Usuario o amigo no válido.");
        }

        await NotifyUserViaWebSocket(friendId, "GameInvitation", userId);
        return Ok("Invitación enviada.");
    }

    [HttpPost("accept-invitation")]
    public async Task<IActionResult> AcceptInvitation(string hostId)
    {
        var userId = User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(hostId))
        {
            return BadRequest("Host o usuario no válido.");
        }

        await NotifyUserViaWebSocket(hostId, "InvitationAccepted", userId);
        return Ok("Invitación aceptada.");
    }

    [HttpPost("join-random-match")]
    public async Task<IActionResult> JoinRandomMatch()
    {
        var userId = User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("Usuario no válido.");
        }

        var opponent = await _gameService.FindRandomOpponentAsync(userId);

        if (opponent == null)
        {
            return Ok("Esperando a un oponente...");
        }

        await NotifyUserViaWebSocket(opponent.Data.GameId.ToString(), "Matched", userId);

        return Ok(new { OpponentId = opponent.Data.GameId.ToString() });
    }

    [HttpPost("play-with-bot")]
    public async Task<IActionResult> PlayWithBot()
    {
        var userId = User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("Usuario no válido.");
        }

        var result = await _gameService.CreateBotGameAsync(userId);

        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return Ok(result.Data);
    }
}