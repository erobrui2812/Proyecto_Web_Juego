using hundir_la_flota.Services;
using Microsoft.AspNetCore.Mvc;

namespace hundir_la_flota.Controllers
{
    [ApiController]
    [Route("api/stats")]
    public class StatsController : ControllerBase
    {
        private readonly IStatsService _statsService;
        private readonly IWebSocketService _webSocketService;

        public StatsController(IStatsService statsService, IWebSocketService webSocketService)
        {
            _statsService = statsService;
            _webSocketService = webSocketService;
        }

        [HttpGet("player/{userId}")]
        public async Task<IActionResult> GetPlayerStats(int userId)
        {
            var stats = await _statsService.GetPlayerStatsAsync(userId);
            if (!stats.Success)
                return NotFound(new { success = false, message = stats.Message });

            return Ok(stats.Data);
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            var leaderboard = await _statsService.GetLeaderboardAsync();
            if (!leaderboard.Success)
                return BadRequest(new { success = false, message = leaderboard.Message });

            return Ok(leaderboard.Data);
        }

        [HttpGet("global")]
        public IActionResult GetLiveStats()
        {
            var stats = new
            {
                OnlineUsers = _webSocketService.GetOnlineUsersCount()
            };
            return Ok(stats);
        }
    }
}
