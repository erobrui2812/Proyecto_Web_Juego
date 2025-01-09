using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class FriendshipController : ControllerBase
{
    private readonly MyDbContext _dbContext;

    public FriendshipController(MyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // 1. Enviar una solicitud de amistad
    [HttpPost("send")]
    public async Task<IActionResult> SendFriendRequest([FromBody] FriendRequestDto request)
    {
        var userId = GetUserId();  

        
        if (string.IsNullOrEmpty(request.Nickname) && string.IsNullOrEmpty(request.Email))
            return BadRequest("Debes proporcionar un nickname o un correo electrónico.");

        
        User friend = null;
        if (!string.IsNullOrEmpty(request.Nickname))
        {
            friend = await _dbContext.Users.FirstOrDefaultAsync(u => u.Nickname == request.Nickname);
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

        // Crear la solicitud de amistad
        var friendship = new Friendship
        {
            UserId = userId,
            FriendId = friendId,
            IsConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Friendships.Add(friendship);
        await _dbContext.SaveChangesAsync();
        return Ok("Solicitud de amistad enviada.");
    }

    // 2. Aceptar o rechazar una solicitud de amistad
    [HttpPost("respond")]
    public async Task<IActionResult> RespondToFriendRequest([FromBody] FriendRequestResponseDto response)
    {
        var userId = GetUserId();

        var friendship = await _dbContext.Friendships
            .FirstOrDefaultAsync(f => f.UserId == response.SenderId && f.FriendId == userId && !f.IsConfirmed);

        if (friendship == null)
            return NotFound("Solicitud de amistad no encontrada.");

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
        return Ok(response.Accept ? "Solicitud de amistad aceptada." : "Solicitud de amistad rechazada.");
    }

    // 3. Obtener la lista de amigos
    [HttpGet("list")]
    public async Task<IActionResult> GetFriends()
    {
        var userId = GetUserId();

        var friends = await _dbContext.Friendships
            .Where(f => (f.UserId == userId || f.FriendId == userId) && f.IsConfirmed)
            .Select(f => new
            {
                FriendId = f.UserId == userId ? f.FriendId : f.UserId,
                FriendNickname = f.UserId == userId ? f.Friend.Nickname : f.User.Nickname,
                AvatarUrl = f.UserId == userId ? f.Friend.AvatarUrl : f.User.AvatarUrl,
                Status = "Desconectado"
            })
            .ToListAsync();

        return Ok(friends);
    }

    // 4. Eliminar un amigo
    [HttpDelete("remove")]
    public async Task<IActionResult> RemoveFriend([FromBody] int friendId)
    {
        var userId = GetUserId();

        var friendship = await _dbContext.Friendships
            .FirstOrDefaultAsync(f =>
                (f.UserId == userId && f.FriendId == friendId) ||
                (f.UserId == friendId && f.FriendId == userId) && f.IsConfirmed);

        if (friendship == null)
            return NotFound("No se encontró la amistad.");

        _dbContext.Friendships.Remove(friendship);
        await _dbContext.SaveChangesAsync();
        return Ok("Amigo eliminado.");
    }

    // 5. Buscar un usuario por nickname
    [HttpGet("search")]
    public async Task<IActionResult> SearchUser([FromQuery] string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
            return BadRequest("El nickname no puede estar vacío.");

        var user = await _dbContext.Users
            .Where(u => u.Nickname.Contains(nickname))
            .Select(u => new
            {
                u.Id,
                u.Nickname,
                u.AvatarUrl
            })
            .ToListAsync();

        if (user.Count == 0)
            return NotFound("No se encontraron usuarios con ese nickname.");

        return Ok(user);
    }

    // Método auxiliar para obtener el ID del usuario autenticado
    private int GetUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId");

        if (userIdClaim == null)
        {
            throw new InvalidOperationException("No se puede obtener el ID del usuario desde el token.");
        }

        Console.WriteLine($"userIdClaim: {userIdClaim.Value}");
        return int.Parse(userIdClaim.Value);
    }

}



// DTO para responder solicitudes de amistad
public class FriendRequestResponseDto
{
    public int SenderId { get; set; }
    public bool Accept { get; set; }
}
