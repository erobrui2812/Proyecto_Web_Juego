using hundir_la_flota.Models;
using hundir_la_flota.DTOs;
using Microsoft.EntityFrameworkCore;
namespace hundir_la_flota.Services
{
    public interface IStatsService
    {
        Task<ServiceResponse<PlayerStatsDTO>> GetPlayerStatsAsync(int userId);
        Task<ServiceResponse<List<LeaderboardDTO>>> GetLeaderboardAsync();
    }
    public class StatsService : IStatsService
    {
        private readonly MyDbContext _context;
        public StatsService(MyDbContext context)
        {
            _context = context;
        }
        public async Task<ServiceResponse<PlayerStatsDTO>> GetPlayerStatsAsync(int userId)
        {
            var playerStats = await _context.PlayerStats
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (playerStats == null)
            {
                playerStats = new PlayerStats { UserId = userId, GamesPlayed = 0, GamesWon = 0, GamesLost = 0 };
                _context.PlayerStats.Add(playerStats);
                await _context.SaveChangesAsync();
            }

            return new ServiceResponse<PlayerStatsDTO>
            {
                Success = true,
                Data = new PlayerStatsDTO
                {
                    UserId = playerStats.UserId,
                    Nickname = _context.Users.Where(u => u.Id == playerStats.UserId).Select(u => u.Nickname).FirstOrDefault(),
                    GamesPlayed = playerStats.GamesPlayed,
                    GamesWon = playerStats.GamesWon
                }
            };
        }

        public async Task<ServiceResponse<List<LeaderboardDTO>>> GetLeaderboardAsync()
        {
            var leaderboard = await _context.GameParticipants
                .Where(gp => gp.Game.State == GameState.Finished)
                .GroupBy(gp => gp.UserId)
                .Select(g => new LeaderboardDTO
                {
                    UserId = g.Key,
                    Nickname = _context.Users.Where(u => u.Id == g.Key).Select(u => u.Nickname).FirstOrDefault(),
                    GamesWon = g.Count(gp => gp.Game.WinnerId == g.Key),
                    TotalGames = _context.GameParticipants.Count(p => p.UserId == g.Key)
                })
                .OrderByDescending(l => l.GamesWon)
                .Take(10)
                .ToListAsync();
            return new ServiceResponse<List<LeaderboardDTO>> { Success = true, Data = leaderboard };
        }
    }
}
