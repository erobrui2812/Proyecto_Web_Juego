using hundir_la_flota.DTOs;
using hundir_la_flota.Models;
using Microsoft.EntityFrameworkCore;

namespace hundir_la_flota.Services
{
    public interface IStatsService
    {
        Task<ServiceResponse<PlayerStatsDTO>> GetPlayerStatsAsync(int userId);
        Task<ServiceResponse<List<LeaderboardDTO>>> GetLeaderboardAsync();
        Task<ServiceResponse<List<ActiveGamePlayersDTO>>> GetPlayersInActiveGamesAsync();
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
                    Nickname = await _context.Users.Where(u => u.Id == playerStats.UserId)
                                       .Select(u => u.Nickname)
                                       .FirstOrDefaultAsync(),
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

        public async Task<ServiceResponse<List<ActiveGameDTO>>> GetActiveGamesAsync()
        {
           
            var activeGames = await _context.Games
                .Include(g => g.Participants)
                    .ThenInclude(p => p.User)
                .Where(g => g.State != GameState.Finished && g.State != GameState.WaitingForPlayers)
                .ToListAsync();


            var activeGamesDto = activeGames.Select(g => new ActiveGameDTO
            {
                GameId = g.GameId,
                StateDescription = g.State switch
                {
                    GameState.WaitingForPlayer1Ships => "El anfitrión está colocando sus barcos.",
                    GameState.WaitingForPlayer2Ships => "El invitado está colocando sus barcos.",
                    GameState.WaitingForPlayer1Shot => "Esperando disparo del anfitrión.",
                    GameState.WaitingForPlayer2Shot => "Esperando disparo del invitado.",
                    GameState.InProgress => "La partida está en progreso.",
                    _ => "Estado desconocido."
                },
                CreatedAt = g.CreatedAt,
                Participants = g.Participants
                    .Select(p => new ParticipantDTO
                    {
                        UserId = p.UserId,
                        Nickname = p.User != null ? p.User.Nickname : (p.UserId == -1 ? "Bot" : "Vacante"),
                        Role = p.Role.ToString()
                    })
                    .ToList()
            }).ToList();

            return new ServiceResponse<List<ActiveGameDTO>> { Success = true, Data = activeGamesDto };
        }
    

    public async Task<ServiceResponse<List<ActiveGamePlayersDTO>>> GetPlayersInActiveGamesAsync()
        {

            var activeGames = await _context.Games
                .Include(g => g.Participants)
                    .ThenInclude(p => p.User)
                .Where(g => g.State != GameState.Finished && g.State != GameState.WaitingForPlayers)
                .ToListAsync();


            var activeGamesDto = activeGames.Select(g =>
            {

                var hostParticipant = g.Participants.FirstOrDefault(p => p.Role == ParticipantRole.Host);
                var guestParticipant = g.Participants.FirstOrDefault(p => p.Role == ParticipantRole.Guest);

                return new ActiveGamePlayersDTO
                {
                    GameId = g.GameId,
                    Player1 = hostParticipant != null
                        ? new ParticipantDTO
                        {
                            UserId = hostParticipant.UserId,
                            Nickname = hostParticipant.User?.Nickname ?? "Vacante",
                            Role = hostParticipant.Role.ToString()
                        }
                        : new ParticipantDTO { UserId = 0, Nickname = "Vacante", Role = "Host" },
                    Player2 = guestParticipant != null
                        ? new ParticipantDTO
                        {
                            UserId = guestParticipant.UserId,
                            Nickname = guestParticipant.User?.Nickname ?? "Vacante",
                            Role = guestParticipant.Role.ToString()
                        }
                        : new ParticipantDTO { UserId = 0, Nickname = "Vacante", Role = "Guest" }
                };
            }).ToList();

            return new ServiceResponse<List<ActiveGamePlayersDTO>> { Success = true, Data = activeGamesDto };
        }

    }

}
