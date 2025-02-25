using hundir_la_flota.Models;
using hundir_la_flota.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata;
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
            var invitationId = Guid.NewGuid().ToString();

            InvitationStorage.PendingInvitations[invitationId] = new Invitation
            {
                HostId = currentUserId,
                GuestId = friendId,
                CreatedAt = DateTime.UtcNow
            };

            await _webSocketService.NotifyUserAsync(friendId, "GameInvitation", invitationId);

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
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
    {
        var userId = GetUserIdFromClaim();

        if (!InvitationStorage.PendingInvitations.TryGetValue(request.InvitationId, out var invitation))
        {
            return BadRequest("La invitación no existe o expiró.");
        }

        if (invitation.GuestId != userId)
        {
            return BadRequest("No tienes permiso para aceptar esta invitación.");
        }

        var createGameResponse = await _gameService.CreateGameAsync(invitation.HostId.ToString());
        if (!createGameResponse.Success)
            return BadRequest(createGameResponse.Message);

        var newGame = createGameResponse.Data;

        var joinResponse = await _gameService.JoinGameAsync(newGame.GameId, userId);
        if (!joinResponse.Success)
            return BadRequest(joinResponse.Message);

        await _webSocketService.NotifyUserAsync(
           invitation.HostId,
           "MatchFound",
           newGame.GameId.ToString()
        );

        InvitationStorage.PendingInvitations.Remove(request.InvitationId);

        return Ok(new { GameId = newGame.GameId });
    }

    public class AcceptInvitationRequest
    {
        public string InvitationId { get; set; }
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
    [HttpPost("rematch")]
    public async Task<IActionResult> Rematch([FromBody] RematchRequest request)
    {
        var userId = GetUserIdFromClaim();
        var response = await _gameService.RematchAsync(request.GameId, request.PlayerId);
        if (!response.Success)
            return BadRequest(response.Message);
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
public class RematchRequest
{
    [Required]
    public Guid GameId { get; set; }
    [Required]
    public int PlayerId { get; set; }
}
