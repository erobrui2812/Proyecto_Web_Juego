//using hundir_la_flota.Models;
//using hundir_la_flota.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System.Net.WebSockets;
//using System.Text;
//using System.Threading;

//[ApiController]
//[Route("api/game")]
//[Authorize]
//public class GameController : ControllerBase
//{
//    private readonly IGameService _gameService;
//    private readonly WebSocketService _webSocketService;

//    public GameController(IGameService gameService, WebSocketService webSocketService)
//    {
//        _gameService = gameService;
//        _webSocketService = webSocketService;
//    }

//    private int GetUserIdFromClaim()
//    {
//        var userIdClaim = User.FindFirst("id")?.Value;
//        if (string.IsNullOrEmpty(userIdClaim))
//            throw new InvalidOperationException("No se encontró el ID del usuario en el token.");
//        return int.Parse(userIdClaim);
//    }

//    private async Task NotifyUserViaWebSocket(int userId, string action, string payload)
//    {
//        if (_webSocketService._connectedUsers.TryGetValue(userId, out var webSocket))
//        {
//            var message = $"{action}|{payload}";
//            var bytes = Encoding.UTF8.GetBytes(message);
//            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
//        }
//    }

//    [HttpPost("create")]
//    public async Task<IActionResult> CreateGame()
//    {
//        var userIdInt = GetUserIdFromClaim();
//        var userIdString = userIdInt.ToString();
//        var result = await _gameService.CreateGameAsync(userIdString);
//        if (!result.Success) return BadRequest(result.Message);
//        return Ok(result.Data);
//    }

//    [HttpPost("{gameId}/join")]
//    public IActionResult JoinGame(Guid gameId, int playerId)
//    {
//        var result = _gameService.JoinGameAsync(gameId, playerId).Result;
//        if (!result.Success) return BadRequest(result.Message);
//        return Ok(result.Message);
//    }

//    [HttpPost("{gameId}/place-ships")]
//    public IActionResult PlaceShips(Guid gameId, int playerId, List<Ship> ships)
//    {
//        var result = _gameService.PlaceShipsAsync(gameId, playerId, ships).Result;
//        if (!result.Success) return BadRequest(result.Message);
//        return Ok(result.Message);
//    }

//    [HttpPost("{gameId}/attack")]
//    public IActionResult Attack(Guid gameId, int playerId, int x, int y)
//    {
//        var result = _gameService.AttackAsync(gameId, playerId, x, y).Result;
//        if (!result.Success) return BadRequest(result.Message);
//        return Ok(result.Message);
//    }

//    [HttpGet("{gameId}")]
//    public async Task<IActionResult> GetGameState(Guid gameId)
//    {
//        var userIdInt = GetUserIdFromClaim();
//        var userIdString = userIdInt.ToString();
//        var result = await _gameService.GetGameStateAsync(userIdString, gameId);
//        if (!result.Success) return BadRequest(result.Message);
//        return Ok(result.Data);
//    }

//    [HttpPost("invite")]
//    public async Task<IActionResult> InviteFriend(int friendId)
//    {
//        var currentUserId = GetUserIdFromClaim();
//        await NotifyUserViaWebSocket(friendId, "GameInvitation", currentUserId.ToString());
//        return Ok("Invitación enviada.");
//    }

//    [HttpPost("accept-invitation")]
//    public async Task<IActionResult> AcceptInvitation(int hostId)
//    {
//        var currentUserId = GetUserIdFromClaim();
//        await NotifyUserViaWebSocket(hostId, "InvitationAccepted", currentUserId.ToString());
//        return Ok("Invitación aceptada.");
//    }

//    [HttpPost("join-random-match")]
//    public async Task<IActionResult> JoinRandomMatch()
//    {
//        var userIdInt = GetUserIdFromClaim();
//        var userIdString = userIdInt.ToString();
//        var opponent = await _gameService.FindRandomOpponentAsync(userIdString);
//        if (opponent == null) return Ok("Esperando a un oponente...");
//        await NotifyUserViaWebSocket(opponent.Data.GameId, "Matched", userIdString);
//        return Ok(new { OpponentId = opponent.Data.GameId });
//    }

//    [HttpPost("play-with-bot")]
//    public async Task<IActionResult> PlayWithBot()
//    {
//        var userIdInt = GetUserIdFromClaim();
//        var userIdString = userIdInt.ToString();
//        var result = await _gameService.CreateBotGameAsync(userIdString);
//        if (!result.Success) return BadRequest(result.Message);
//        return Ok(result.Data);
//    }
//}
