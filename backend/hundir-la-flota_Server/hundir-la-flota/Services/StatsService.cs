using hundir_la_flota.Models;
using hundir_la_flota.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace hundir_la_flota.Services
{
    public interface IStatsService
    {
        Task<ServiceResponse<PlayerStatsDTO>> GetPlayerStatsAsync(int userId);
        Task<ServiceResponse<List<LeaderboardDTO>>> GetLeaderboardAsync();
        Task<ServiceResponse<GlobalStatsDTO>> GetGlobalStatsAsync();
    }

    public class StatsService : IStatsService
    {
        private readonly MyDbContext _context;
        private readonly IWebSocketService _webSocketService;
        private readonly IGameRepository _gameRepository;

        public StatsService(MyDbContext context, IWebSocketService webSocketService, IGameRepository gameRepository)
        {
            _context = context;
            _webSocketService = webSocketService;
            _gameRepository = gameRepository;
        }

        public async Task<ServiceResponse<PlayerStatsDTO>> GetPlayerStatsAsync(int userId)
        {
            var playerStats = await _context.GameParticipants
                .Where(gp => gp.UserId == userId)
                .GroupBy(gp => gp.UserId)
                .Select(g => new PlayerStatsDTO
                {
                    UserId = g.Key,
                    Nickname = _context.Users.Where(u => u.Id == g.Key).Select(u => u.Nickname).FirstOrDefault(),
                    GamesPlayed = g.Count(),
                    GamesWon = g.Count(gp => gp.Game.State == GameState.Finished && gp.Game.WinnerId == userId)
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

        public async Task<ServiceResponse<GlobalStatsDTO>> GetGlobalStatsAsync()
        {
            var onlineUsers = _webSocketService.GetConnectedUserIds().Count;
            var playingUsers = _webSocketService.GetConnectedUserIds()
                .Count(userId => _webSocketService.GetUserState(userId) == WebSocketService.UserState.Playing);

            var activeGames = (await _gameRepository.GetAllAsync())
                .Count(g => g.State != GameState.Finished && g.State != GameState.WaitingForPlayers);

            var stats = new GlobalStatsDTO
            {
                OnlineUsers = onlineUsers,
                PlayersInGame = playingUsers,
                ActiveGames = activeGames
            };

            return new ServiceResponse<GlobalStatsDTO> { Success = true, Data = stats };
        }
    }
}
