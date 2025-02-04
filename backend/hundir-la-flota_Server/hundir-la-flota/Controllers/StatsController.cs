using hundir_la_flota.Models;
using hundir_la_flota.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;
    private readonly IWebSocketService _webSocketService;
    private readonly MyDbContext _context;
    public StatsController(IStatsService statsService, IWebSocketService webSocketService, MyDbContext context)
    {
        _statsService = statsService;
        _webSocketService = webSocketService;
        _context = context;
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
        var stats = new { OnlineUsers = _webSocketService.GetOnlineUsersCount() };
        return Ok(stats);
    }
    [HttpGet("games")]
    public async Task<IActionResult> GetActiveGames()
    {
        var count = await _context.Games.CountAsync(g => g.State != GameState.Finished && g.State != GameState.WaitingForPlayers);
        return Ok(new { ActiveGames = count });
    }
    [HttpGet("players")]
    public async Task<IActionResult> GetPlayersInGame()
    {
        var count = await _context.GameParticipants.CountAsync();
        return Ok(new { PlayersInGame = count });
    }
}
