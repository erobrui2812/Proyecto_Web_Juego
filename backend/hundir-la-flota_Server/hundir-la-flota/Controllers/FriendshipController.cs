using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Net.WebSockets;
using hundir_la_flota.Services;
using hundir_la_flota.DTOs;

[ApiController]
[Route("api/[controller]")]
public class FriendshipController : ControllerBase
{
    private readonly MyDbContext _dbContext;
    private readonly WebSocketService _webSocketService;

    public FriendshipController(MyDbContext dbContext, WebSocketService webSocketService)
    {
        _dbContext = dbContext;
        _webSocketService = webSocketService;
    }

    private async Task NotifyUserViaWebSocket(int userId, string action, string payload)
    {
        if (_webSocketService._connectedUsers.TryGetValue(userId, out var webSocket))
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
            var normalizedNickname = NormalizeString(request.Nickname).Trim();
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

        await NotifyUserViaWebSocket(friendId, "FriendRequest", userId.ToString());
        return Ok("Solicitud de amistad enviada.");
    }

    [HttpPost("add-{userId}")]
    public async Task<IActionResult> AddFriendById(int userId)
    {
        var currentUserId = GetUserId();
        if (currentUserId == userId)
            return BadRequest("No puedes enviarte una solicitud de amistad a ti mismo.");

        var friend = await _dbContext.Users.FindAsync(userId);
        if (friend == null)
            return NotFound("No se encontró el usuario.");

        var existingFriendship = await _dbContext.Friendships
            .FirstOrDefaultAsync(f =>
                (f.UserId == currentUserId && f.FriendId == userId) ||
                (f.UserId == userId && f.FriendId == currentUserId));

        if (existingFriendship != null)
            return BadRequest("Ya existe una solicitud de amistad o ya sois amigos.");

        var friendship = new Friendship
        {
            UserId = currentUserId,
            FriendId = userId,
            IsConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Friendships.Add(friendship);
        await _dbContext.SaveChangesAsync();

        await NotifyUserViaWebSocket(userId, "FriendRequest", currentUserId.ToString());
        return Ok("Solicitud de amistad enviada.");
    }

    [HttpPost("respond")]
    public async Task<IActionResult> RespondToFriendRequest([FromBody] FriendRequestResponseDto response)
    {
        var userId = GetUserId();

        var friendship = await _dbContext.Friendships
            .FirstOrDefaultAsync(f =>
                f.UserId == response.SenderId &&
                f.FriendId == userId &&
                !f.IsConfirmed);

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

        var responseMessage = response.Accept ? "Accepted" : "Rejected";
        await NotifyUserViaWebSocket(response.SenderId, "FriendRequestResponse", responseMessage);
        return Ok(new
        {
            success = true,
            message = response.Accept ? "Solicitud de amistad aceptada." : "Solicitud de amistad rechazada."
        });
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetFriends()
    {
        var userId = GetUserId();

        var friendships = await _dbContext.Friendships
            .Where(f => (f.UserId == userId || f.FriendId == userId) && f.IsConfirmed)
            .Select(f => new
            {
                FriendId = f.UserId == userId ? f.FriendId : f.UserId,
                FriendNickname = f.UserId == userId ? f.Friend.Nickname : f.User.Nickname,
                FriendMail = f.UserId == userId ? f.Friend.Email : f.User.Email,
                AvatarUrl = f.UserId == userId ? f.Friend.AvatarUrl : f.User.AvatarUrl
            })
            .ToListAsync();

        var webSocketService = HttpContext.RequestServices.GetService<WebSocketService>();
        if (webSocketService == null)
            return StatusCode(StatusCodes.Status500InternalServerError, "WebSocketService no disponible.");

        var friendsWithStatus = friendships.Select(f => new
        {
            f.FriendId,
            f.FriendNickname,
            f.FriendMail,
            f.AvatarUrl,
            Status = webSocketService._userStates.TryGetValue(f.FriendId, out var state)
                ? state.ToString()
                : WebSocketService.UserState.Disconnected.ToString()
        }).ToList();

        return Ok(friendsWithStatus);
    }

    [HttpDelete("remove")]
    public async Task<IActionResult> RemoveFriend([FromBody] int friendId)
    {
        var userId = GetUserId();

        var friendship = await _dbContext.Friendships
            .FirstOrDefaultAsync(f =>
                (f.UserId == userId && f.FriendId == friendId) ||
                (f.UserId == friendId && f.FriendId == userId)
                && f.IsConfirmed);

        if (friendship == null)
            return NotFound("No se encontró la amistad.");

        _dbContext.Friendships.Remove(friendship);
        await _dbContext.SaveChangesAsync();

        return Ok("Amigo eliminado.");
    }

    [HttpGet("unaccepted")]
    public async Task<IActionResult> GetUnacceptedFriendRequests()
    {
        var userId = GetUserId();

        var unacceptedRequests = await _dbContext.Friendships
            .Where(f => f.UserId == userId && !f.IsConfirmed)
            .Include(f => f.Friend)
            .ToListAsync();

        var result = unacceptedRequests.Select(f => new
        {
            f.Id,
            ToUserId = f.FriendId,
            ToUserNickname = f.Friend.Nickname,
            CreatedAt = f.CreatedAt
        });

        return Ok(result);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingFriendRequests()
    {
        var userId = GetUserId();

        var pendingRequests = await _dbContext.Friendships
            .Where(f => f.FriendId == userId && !f.IsConfirmed)
            .Include(f => f.User)
            .ToListAsync();

        var result = pendingRequests.Select(f => new
        {
            f.Id,
            FromUserId = f.UserId,
            FromUserNickname = f.User.Nickname,
            CreatedAt = f.CreatedAt
        });

        return Ok(result);
    }

    [HttpGet("search")]
    public IActionResult SearchUser([FromQuery] string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
            return BadRequest("El nickname no puede estar vacío.");

        var normalizedSearch = NormalizeString(nickname).Trim();


        var users = _dbContext.Users
            .AsEnumerable()
            .Where(u => NormalizeString(u.Nickname).Contains(normalizedSearch))
            .Select(u => new
            {
                u.Id,
                u.Nickname,
                u.AvatarUrl
            })
            .ToList();

        if (users.Count == 0)
            return NotFound("No se encontraron usuarios con ese nickname.");

        return Ok(users);
    }

    [HttpGet("get-nickname/{userId}")]
    public async Task<IActionResult> GetNickname(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { success = false, message = "Usuario no encontrado." });

        return Ok(new { success = true, nickname = user.Nickname });
    }

    private int GetUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id");
        if (userIdClaim == null)
            throw new InvalidOperationException("No se puede obtener el ID del usuario desde el token.");

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
