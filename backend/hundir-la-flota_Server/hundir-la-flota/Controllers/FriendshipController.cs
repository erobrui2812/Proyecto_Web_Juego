using hundir_la_flota.DTOs;
using hundir_la_flota.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class FriendshipController : ControllerBase
{
    private readonly IFriendshipService _friendshipService;

    public FriendshipController(IFriendshipService friendshipService)
    {
        _friendshipService = friendshipService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendFriendRequest([FromBody] FriendRequestDto request)
    {
        var userId = GetUserId();
        var result = await _friendshipService.SendFriendRequestAsync(userId, request);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result.Message);
    }

    [HttpPost("respond")]
    public async Task<IActionResult> RespondToFriendRequest([FromBody] FriendRequestResponseDto response)
    {
        var userId = GetUserId();
        var result = await _friendshipService.RespondToFriendRequestAsync(userId, response);
        if (!result.Success) return NotFound(new { success = false, message = result.Message });
        return Ok(new { success = true, message = result.Message });
    }

    [HttpDelete("remove/{friendId}")]
    public async Task<IActionResult> RemoveFriend(int friendId)
    {
        var userId = GetUserId();
        var result = await _friendshipService.RemoveFriendAsync(userId, friendId);
        if (!result.Success) return NotFound(result.Message);
        return Ok(result.Message);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetFriends()
    {
        var userId = GetUserId();
        var result = await _friendshipService.GetFriendsAsync(userId);
        if (!result.Success) return StatusCode(StatusCodes.Status500InternalServerError, result.Message);
        return Ok(result.Data);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingFriendRequests()
    {
        var userId = GetUserId();
        var result = await _friendshipService.GetPendingRequestsAsync(userId);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result.Data);
    }

    [HttpGet("unaccepted")]
    public async Task<IActionResult> GetUnacceptedFriendRequests()
    {
        var userId = GetUserId();
        var result = await _friendshipService.GetUnacceptedRequestsAsync(userId);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result.Data);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string nickname)
    {
        if (string.IsNullOrEmpty(nickname)) return BadRequest("El nickname no puede estar vacío.");
        var result = await _friendshipService.SearchUsersAsync(nickname);
        if (!result.Success) return NotFound(result.Message);
        return Ok(result.Data);
    }

    [HttpGet("get-nickname/{userId}")]
    public async Task<IActionResult> GetNickname(int userId)
    {
        var result = await _friendshipService.GetNicknameAsync(userId);
        if (!result.Success) return NotFound(new { success = false, message = result.Message });
        return Ok(new { success = true, nickname = result.Data });
    }

    [HttpGet("connected")]
    public async Task<IActionResult> GetConnectedFriends()
    {
        var userId = GetUserId();
        var result = await _friendshipService.GetConnectedFriendsAsync(userId);
        if (!result.Success) return StatusCode(StatusCodes.Status500InternalServerError, result.Message);
        return Ok(result.Data);
    }


    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("No se puede obtener el ID del usuario desde el token.");
        }
        return int.Parse(userIdClaim);
    }

}
