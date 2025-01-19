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
            var playerStats = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new PlayerStatsDTO
                {
                    UserId = u.Id,
                    Nickname = u.Nickname,
                    GamesPlayed = _context.Games.Count(g => g.Player1Id == u.Id || g.Player2Id == u.Id),
                    GamesWon = _context.Games.Count(g => g.State == GameState.Finished && g.CurrentPlayerId == u.Id),
                })
                .FirstOrDefaultAsync();

            if (playerStats == null)
            {
                return new ServiceResponse<PlayerStatsDTO>
                {
                    Success = false,
                    Message = "No se encontraron estadísticas para el jugador."
                };
            }

            return new ServiceResponse<PlayerStatsDTO> { Success = true, Data = playerStats };
        }

        public async Task<ServiceResponse<List<LeaderboardDTO>>> GetLeaderboardAsync()
        {
            var leaderboard = await _context.Users
                .Select(u => new LeaderboardDTO
                {
                    UserId = u.Id,
                    Nickname = u.Nickname,
                    GamesWon = _context.Games.Count(g => g.State == GameState.Finished && g.CurrentPlayerId == u.Id),
                    TotalGames = _context.Games.Count(g => g.Player1Id == u.Id || g.Player2Id == u.Id),
                })
                .OrderByDescending(l => l.GamesWon)
                .Take(10)
                .ToListAsync();

            return new ServiceResponse<List<LeaderboardDTO>> { Success = true, Data = leaderboard };
        }
    }
}
