using hundir_la_flota.Models;
using hundir_la_flota.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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
        {
            throw new UnauthorizedAccessException("No se encontró el ID del usuario en el token.");
        }
        return int.Parse(userIdClaim);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateGame()
    {
        try
        {
            var userId = GetUserIdFromClaim();
            var response = await _gameService.CreateGameAsync(userId.ToString());
            if (!response.Success)
                return BadRequest(response.Message);

            return Ok(response.Data);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{gameId}/abandon")]
    public async Task<IActionResult> AbandonGame(Guid gameId)
    {
        var userId = GetUserIdFromClaim();
        var response = await _gameService.AbandonGameAsync(gameId, userId);
        if (!response.Success)
            return BadRequest(response.Message);

        await _webSocketService.NotifyUserStatusChangeAsync(userId, WebSocketService.UserState.Connected);
        return Ok("Juego abandonado correctamente.");
    }

    [HttpPost("{gameId}/reassign")]
    public async Task<IActionResult> ReassignRoles(Guid gameId)
    {
        var response = await _gameService.ReassignRolesAsync(gameId);
        if (!response.Success)
            return BadRequest(response.Message);

        return Ok("Roles reasignados correctamente.");
    }

    [HttpPost("{gameId}/join")]
    public async Task<IActionResult> JoinGame(Guid gameId, [FromBody] int playerId)
    {
        var response = await _gameService.JoinGameAsync(gameId, playerId);
        if (!response.Success)
            return BadRequest(response.Message);

        await _webSocketService.NotifyUserStatusChangeAsync(playerId, WebSocketService.UserState.Playing);
        return Ok(response.Message);
    }

    [HttpPost("{gameId}/place-ships")]
    public async Task<IActionResult> PlaceShips(Guid gameId, [FromBody] PlaceShipsRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

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
        var response = await _gameService.GetGameStateAsync(GetUserIdFromClaim().ToString(), gameId);
        if (!response.Success)
            return BadRequest(response.Message);

        return Ok(response.Data);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteFriend([FromBody] int friendId)
    {
        try
        {
            var currentUserId = GetUserIdFromClaim();

            var createGameResponse = await _gameService.CreateGameAsync(currentUserId.ToString());
            if (!createGameResponse.Success)
            {
                return BadRequest(createGameResponse.Message);
            }

            var newGame = createGameResponse.Data;

            string payload = $"{currentUserId}|{newGame.GameId}";

            await _webSocketService.NotifyUserAsync(friendId, "GameInvitation", payload);

            return Ok("Invitación enviada.");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }


    [HttpPost("accept-invitation")]
    public async Task<IActionResult> AcceptInvitation([FromBody] Guid gameId)
    {
        var userId = GetUserIdFromClaim();
        var response = await _gameService.JoinGameAsync(gameId, userId);

        if (!response.Success)
            return BadRequest(response.Message);

        return Ok(response.Message);
    }

    [HttpPost("join-random-match")]
    public async Task<IActionResult> JoinRandomMatch()
    {
        var userId = GetUserIdFromClaim();
        var response = await _gameService.FindRandomOpponentAsync(userId.ToString());
        if (!response.Success)
            return Ok("Esperando a un oponente...");

        await _webSocketService.NotifyUserStatusChangeAsync(userId, WebSocketService.UserState.Playing);
        return Ok(new { OpponentId = response.Data.GameId });
    }

    [HttpPost("play-with-bot")]
    public async Task<IActionResult> PlayWithBot()
    {
        var userId = GetUserIdFromClaim();
        var response = await _gameService.CreateBotGameAsync(userId.ToString());
        if (!response.Success)
            return BadRequest(response.Message);

        await _webSocketService.NotifyUserStatusChangeAsync(userId, WebSocketService.UserState.Playing);
        return Ok(response.Data);
    }

    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        var userId = GetUserIdFromClaim();
        await _webSocketService.DisconnectUserAsync(userId);
        return Ok("Usuario desconectado correctamente.");
    }
}

// Clases auxiliares
public class PlaceShipsRequest
{
    [Required]
    public int PlayerId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Debes colocar al menos un barco.")]
    public List<Ship> Ships { get; set; }
}

public class AttackRequest
{
    [Required]
    public int PlayerId { get; set; }

    [Range(0, 9, ErrorMessage = "El valor de X debe estar entre 0 y 9.")]
    public int X { get; set; }

    [Range(0, 9, ErrorMessage = "El valor de Y debe estar entre 0 y 9.")]
    public int Y { get; set; }
}
