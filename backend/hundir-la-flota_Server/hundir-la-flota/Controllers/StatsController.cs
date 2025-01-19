using hundir_la_flota.Services;
using Microsoft.AspNetCore.Mvc;

namespace hundir_la_flota.Controllers
{
    [ApiController]
    [Route("api/stats")]
    public class StatsController : ControllerBase
    {
        private readonly IStatsService _statsService;

        public StatsController(IStatsService statsService)
        {
            _statsService = statsService;
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
    }
}
