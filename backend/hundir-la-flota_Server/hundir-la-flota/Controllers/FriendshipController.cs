using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Globalization;
using System.Net.WebSockets;
using hundir_la_flota.DTOs;

[ApiController]
[Route("api/[controller]")]
public class FriendshipController : ControllerBase
{
    private readonly MyDbContext _dbContext;

    public FriendshipController(MyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private async Task NotifyUserViaWebSocket(int userId, string action, string payload)
    {
        if (WebSocketController.ConnectedUsers.TryGetValue(userId.ToString(), out var webSocket))
        {
            var message = $"{action}|{payload}";
            var bytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendFriendRequest([FromBody] FriendRequestDto request)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(request.Nickname) && string.IsNullOrEmpty(request.Email))
            return BadRequest("Debes proporcionar un nickname o un correo electrónico.");

        User friend = null;
        if (!string.IsNullOrEmpty(request.Nickname))
        {
            var normalizedNickname = NormalizeString(request.Nickname);

            friend = _dbContext.Users
                .AsEnumerable()
                .FirstOrDefault(u => NormalizeString(u.Nickname) == normalizedNickname);
        }

        if (friend == null && !string.IsNullOrEmpty(request.Email))
        {
            friend = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        }

        if (friend == null)
            return NotFound("No se encontró el usuario.");

        var friendId = friend.Id;

        if (userId == friendId)
            return BadRequest("No puedes enviarte una solicitud de amistad a ti mismo.");

        var existingFriendship = await _dbContext.Friendships
            .FirstOrDefaultAsync(f =>
                (f.UserId == userId && f.FriendId == friendId) ||
                (f.UserId == friendId && f.FriendId == userId));

        if (existingFriendship != null)
            return BadRequest("Ya existe una solicitud de amistad o ya sois amigos.");

        var friendship = new Friendship
        {
            UserId = userId,
            FriendId = friendId,
            IsConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Friendships.Add(friendship);
        await _dbContext.SaveChangesAsync();

        await NotifyUserViaWebSocket(friendId, "ReceiveFriendRequest", userId.ToString());

        return Ok("Solicitud de amistad enviada.");
    }

    [HttpPost("respond")]
    public async Task<IActionResult> RespondToFriendRequest([FromBody] FriendRequestResponseDto response)
    {
        var userId = GetUserId();

        var friendship = await _dbContext.Friendships
            .FirstOrDefaultAsync(f => f.UserId == response.SenderId && f.FriendId == userId && !f.IsConfirmed);

        if (friendship == null)
            return NotFound(new { success = false, message = "Solicitud de amistad no encontrada." });

        if (response.Accept)
        {
            friendship.IsConfirmed = true;
            friendship.ConfirmedAt = DateTime.UtcNow;
        }
        else
        {
            _dbContext.Friendships.Remove(friendship);
        }

        await _dbContext.SaveChangesAsync();

        await NotifyUserViaWebSocket(response.SenderId, "FriendRequestResponse", response.Accept.ToString());

        return Ok(new
        {
            success = true,
            message = response.Accept
                ? "Solicitud de amistad aceptada."
                : "Solicitud de amistad rechazada."
        });
    }

    private int GetUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id");

        if (userIdClaim == null)
        {
            throw new InvalidOperationException("No se puede obtener el ID del usuario desde el token.");
        }

        return int.Parse(userIdClaim.Value);
    }

    private string NormalizeString(string input)
    {
        var normalizedString = input.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var character in normalizedString)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(character);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLower();
    }
}