﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System;
using hundir_la_flota.Hubs;

[ApiController]
[Route("api/[controller]")]
public class FriendshipController : ControllerBase
{
    private readonly MyDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;

    public FriendshipController(MyDbContext dbContext, IHubContext<NotificationHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
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
            var normalizedNickname = NormalizeString(request.Nickname);
            // Traemos todos los usuarios y filtramos en memoria
            friend = _dbContext.Users
                .AsEnumerable()  // Esto asegura que la normalización se haga en memoria
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

        // Notificar al usuario amigo sobre la nueva solicitud
        await _hubContext.Clients.User(friendId.ToString()).SendAsync("ReceiveFriendRequest", userId);

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

        // Notificar al usuario que envió la solicitud sobre la respuesta
        await _hubContext.Clients.User(response.SenderId.ToString()).SendAsync("FriendRequestResponse", response.Accept);

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
                Status = "Desconectado" //esto dependerá del websocket
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

        // Notificar a ambos usuarios sobre la eliminación de la amistad
        await _hubContext.Clients.User(friendId.ToString()).SendAsync("FriendRemoved", userId);
        await _hubContext.Clients.User(userId.ToString()).SendAsync("FriendRemoved", friendId);

        return Ok("Amigo eliminado.");
    }

    // 5. Buscar un usuario por nickname
    [HttpGet("search")]
    public async Task<IActionResult> SearchUser([FromQuery] string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
            return BadRequest("El nickname no puede estar vacío.");

        var normalizedNickname = NormalizeString(nickname);

        var users = await _dbContext.Users
            .ToListAsync();

        var filteredUsers = users
            .Where(u => NormalizeString(u.Nickname).Contains(normalizedNickname))
            .Select(u => new
            {
                u.Id,
                u.Nickname,
                u.AvatarUrl
            })
            .ToList();

        if (filteredUsers.Count == 0)
            return NotFound("No se encontraron usuarios con ese nickname.");

        return Ok(filteredUsers);
    }

    private int GetUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            throw new InvalidOperationException("No se puede obtener el ID del usuario desde el token.");
        }

        Console.WriteLine($"userIdClaim: {userIdClaim.Value}");
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

public class FriendRequestResponseDto
{
    public int SenderId { get; set; }
    public bool Accept { get; set; }
}
