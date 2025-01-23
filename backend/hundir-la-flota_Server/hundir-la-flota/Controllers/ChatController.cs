using hundir_la_flota.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hundir_la_flota.Services;


namespace hundir_la_flota.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("{gameId}/send-message")]
        public async Task<IActionResult> SendMessage(Guid gameId, [FromBody] ChatMessageRequest request)
        {
            var userId = GetUserIdFromClaim();
            var response = await _chatService.SendMessageAsync(gameId, userId, request.Message);

            if (!response.Success)
                return BadRequest(response.Message);

            return Ok(response.Message);
        }

        [HttpGet("{gameId}/messages")]
        public async Task<IActionResult> GetMessages(Guid gameId)
        {
            var response = await _chatService.GetMessagesAsync(gameId);
            if (!response.Success)
                return BadRequest(response.Message);

            return Ok(response.Data);
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
    }
}
