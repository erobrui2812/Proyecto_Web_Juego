using hundir_la_flota.DTOs;
using hundir_la_flota.Models;
using hundir_la_flota.Repositories;
using hundir_la_flota.Services;
using hundir_la_flota.Utils;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

public interface IGameService
{
    Task<ServiceResponse<Game>> CreateGameAsync(string userId);
    Task<ServiceResponse<GameResponseDTO>> GetGameStateAsync(string userId, Guid gameId);
    Task<ServiceResponse<string>> JoinGameAsync(Guid gameId, int playerId);
    Task<ServiceResponse<string>> AbandonGameAsync(Guid gameId, int playerId);
    Task<ServiceResponse<string>> ReassignRolesAsync(Guid gameId);
    Task<ServiceResponse<string>> PlaceShipsAsync(Guid gameId, int playerId, List<Ship> ships);
    Task<ServiceResponse<string>> AttackAsync(Guid gameId, int playerId, int x, int y);
    Task<ServiceResponse<Game>> FindRandomOpponentAsync(string userId);
    Task<ServiceResponse<Game>> CreateBotGameAsync(string userId);
    Task<ServiceResponse<string>> HandleDisconnectionAsync(int playerId);
    Task<ServiceResponse<string>> PassTurnAsync(Guid gameId, int playerId);
    Task<ServiceResponse<Game>> RematchAsync(Guid gameId, int playerId);

}

public class GameService : IGameService
{
    private static readonly SemaphoreSlim _turnLock = new(1, 1);
    private readonly IGameRepository _gameRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWebSocketService _webSocketService;
    private readonly IGameParticipantRepository _gameParticipantRepository;
    private readonly MyDbContext _context;


    public GameService(
     IGameRepository gameRepository,
     IUserRepository userRepository,
     IGameParticipantRepository gameParticipantRepository,
     IWebSocketService webSocketService,
     MyDbContext context)

    {
        _gameRepository = gameRepository;
        _userRepository = userRepository;
        _gameParticipantRepository = gameParticipantRepository;
        _webSocketService = webSocketService;
        _context = context;
    }

    public async Task<ServiceResponse<Game>> CreateGameAsync(string userId)
    {
        int userIdInt = Convert.ToInt32(userId);

        var newGame = new Game
        {
            State = GameState.WaitingForPlayers,
            CreatedAt = DateTime.Now
        };

        await _gameRepository.AddAsync(newGame);

        var participant = new GameParticipant
        {
            GameId = newGame.GameId,
            UserId = userIdInt,
            Role = ParticipantRole.Host,
            IsReady = false
        };

        await _gameParticipantRepository.AddAsync(participant);

        return new ServiceResponse<Game> { Success = true, Data = newGame };
    }


    public async Task<ServiceResponse<string>> JoinGameAsync(Guid gameId, int userId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null)
            return new ServiceResponse<string> { Success = false, Message = "La partida no está disponible." };

        var existingParticipants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);
        if (existingParticipants.Any(p => p.UserId == userId))
            return new ServiceResponse<string> { Success = false, Message = "Ya estás en esta partida." };

        if (existingParticipants.Count >= 2)
            return new ServiceResponse<string> { Success = false, Message = "La partida ya está llena." };

        var participant = new GameParticipant
        {
            GameId = gameId,
            UserId = userId,
            Role = ParticipantRole.Guest,
            IsReady = false
        };

        await _gameParticipantRepository.AddAsync(participant);


        if (existingParticipants.Count + 1 >= 2)
        {
            game.State = GameState.WaitingForPlayer1Ships;
            await _gameRepository.UpdateAsync(game);
        }

        return new ServiceResponse<string> { Success = true, Message = "Te has unido a la partida." };
    }

    public async Task<ServiceResponse<string>> AbandonGameAsync(Guid gameId, int userId)
    {
        var participant = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);
        if (participant == null)
            return new ServiceResponse<string> { Success = false, Message = "No estás participando en esta partida." };

        await _gameParticipantRepository.RemoveAsync(participant.FirstOrDefault(p => p.UserId == userId));

        var remainingParticipants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);
        var game = await _gameRepository.GetByIdAsync(gameId);

        if (!remainingParticipants.Any())
        {
            game.State = GameState.WaitingForPlayers;
        }
        else
        {
            game.State = GameState.WaitingForPlayer1Ships;
        }

        await _gameRepository.UpdateAsync(game);
        return new ServiceResponse<string> { Success = true, Message = "Has abandonado la partida." };
    }

    public async Task<ServiceResponse<string>> PlaceShipsAsync(Guid gameId, int userId, List<Ship> ships)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null || game.State == GameState.Finished)
            return new ServiceResponse<string> { Success = false, Message = "No se puede colocar barcos en esta etapa." };

        var participants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);
        var participant = participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
            return new ServiceResponse<string> { Success = false, Message = "Participante no válido." };

        var playerBoard = participant.Role == ParticipantRole.Host
            ? game.Player1Board
            : game.Player2Board;

        if (playerBoard == null)
            return new ServiceResponse<string> { Success = false, Message = "Tablero del jugador no encontrado." };

        foreach (var ship in ships)
        {
            ship.Id = 0;
            if (!playerBoard.PlaceShip(ship))
                return new ServiceResponse<string> { Success = false, Message = $"Posición inválida para el barco {ship.Name}." };
        }

        participant.IsReady = true;
        await _gameParticipantRepository.UpdateAsync(participant);


        if (participants.All(p => p.IsReady))
        {
            game.State = GameState.WaitingForPlayer1Shot;
            await _gameRepository.UpdateAsync(game);


            await _webSocketService.NotifyUsersAsync(
                participants.Select(p => p.UserId),
                "GameStarted",
                "El juego ha comenzado. Es el turno del anfitrión."
            );


            var host = participants.First(p => p.Role == ParticipantRole.Host);
            await _webSocketService.NotifyUserAsync(host.UserId, "YourTurn", "Es tu turno de atacar.");
        }

        await _gameRepository.UpdateAsync(game);
        return new ServiceResponse<string> { Success = true, Message = "Barcos colocados correctamente." };
    }





    public async Task<ServiceResponse<string>> AttackAsync(Guid gameId, int playerId, int x, int y)
    {
        await _turnLock.WaitAsync();
        try
        {
            var game = await _gameRepository.GetByIdAsync(gameId);
            if (game == null)
                return new ServiceResponse<string> { Success = false, Message = "La partida no existe." };

            if (game.State != GameState.WaitingForPlayer1Shot && game.State != GameState.WaitingForPlayer2Shot)
                return new ServiceResponse<string> { Success = false, Message = "No se puede atacar en esta etapa." };

            var participants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);
            var currentPlayer = participants.FirstOrDefault(p => p.UserId == playerId);
            var opponent = participants.FirstOrDefault(p => p.UserId != playerId);

            if (currentPlayer == null || opponent == null)
                return new ServiceResponse<string> { Success = false, Message = "No se encontraron los participantes." };

            if ((game.State == GameState.WaitingForPlayer1Shot && currentPlayer.Role != ParticipantRole.Host) ||
                (game.State == GameState.WaitingForPlayer2Shot && currentPlayer.Role != ParticipantRole.Guest))
            {
                return new ServiceResponse<string> { Success = false, Message = "No es tu turno." };
            }

            var opponentBoard = currentPlayer.Role == ParticipantRole.Host
                ? game.Player2Board
                : game.Player1Board;

            var cell = opponentBoard.Grid
                .Where(kvp => kvp.Key.Item1 == x && kvp.Key.Item2 == y)
                .Select(kvp => kvp.Value)
                .FirstOrDefault();

            if (cell == null || cell.IsHit)
                return new ServiceResponse<string> { Success = false, Message = "Celda inválida o ya atacada." };

            cell.IsHit = true;
            var ship = opponentBoard.Ships.FirstOrDefault(s => s.Coordinates.Any(coord => coord.X == x && coord.Y == y));
            var actionDetails = $"Disparo en ({x}, {y})";

            if (ship != null)
            {
                actionDetails += ship.IsSunk ? " ¡Barco hundido!" : " ¡Acierto!";
                if (opponentBoard.Ships.All(s => s.IsSunk))
                {
                    game.State = GameState.Finished;
                    game.WinnerId = playerId;
                    actionDetails += " Fin del juego.";
                    await _webSocketService.NotifyUsersAsync(new List<int> { playerId, opponent.UserId }, "GameOver", $"El jugador {playerId} ha ganado.");

                    // Actualizar estadísticas de ambos jugadores
                    await UpdatePlayerStats(playerId, opponent.UserId);
                }
            }
            else
            {
                actionDetails += " Fallo.";
            }

            game.State = game.State == GameState.WaitingForPlayer1Shot ? GameState.WaitingForPlayer2Shot : GameState.WaitingForPlayer1Shot;
            await _gameRepository.UpdateAsync(game);

            int nextPlayerId = game.State == GameState.WaitingForPlayer1Shot
                ? participants.First(p => p.Role == ParticipantRole.Host).UserId
                : participants.First(p => p.Role == ParticipantRole.Guest).UserId;

            await _webSocketService.NotifyUserAsync(nextPlayerId, "YourTurn", "Es tu turno de atacar.");

            return new ServiceResponse<string> { Success = true, Message = actionDetails };
        }
        finally
        {
            _turnLock.Release();
        }
    }

    private async Task UpdatePlayerStats(int winnerId, int loserId)
    {

        var winnerStats = await _context.PlayerStats.FirstOrDefaultAsync(s => s.UserId == winnerId);
        if (winnerStats != null)
        {
            winnerStats.GamesWon++;
            winnerStats.GamesPlayed++;
        }
        else
        {
            winnerStats = new PlayerStats { UserId = winnerId, GamesWon = 1, GamesPlayed = 1 };
            _context.PlayerStats.Add(winnerStats);
        }


        var loserStats = await _context.PlayerStats.FirstOrDefaultAsync(s => s.UserId == loserId);
        if (loserStats != null)
        {
            loserStats.GamesLost++;
            loserStats.GamesPlayed++;
        }
        else
        {
            loserStats = new PlayerStats { UserId = loserId, GamesLost = 1, GamesPlayed = 1 };
            _context.PlayerStats.Add(loserStats);
        }

        await _context.SaveChangesAsync();
    }



    private GameAction SimulateBotAttack(Board playerBoard)
    {
        var random = new Random();


        var potentialTargets = new List<Cell>();

        foreach (var ship in playerBoard.Ships)
        {

            if (ship.IsSunk)
                continue;


            foreach (var coord in ship.Coordinates)
            {

                var cell = playerBoard.Grid[(coord.X, coord.Y)];
                if (cell.IsHit)
                {

                    var adjacentCoords = new List<(int X, int Y)>
                {
                    (coord.X - 1, coord.Y),
                    (coord.X + 1, coord.Y),
                    (coord.X, coord.Y - 1),
                    (coord.X, coord.Y + 1)
                };

                    foreach (var adjacent in adjacentCoords)
                    {

                        if (adjacent.X >= 0 && adjacent.X < Board.Size && adjacent.Y >= 0 && adjacent.Y < Board.Size)
                        {
                            var adjacentCell = playerBoard.Grid[(adjacent.X, adjacent.Y)];
                            if (!adjacentCell.IsHit)
                            {
                                potentialTargets.Add(adjacentCell);
                            }
                        }
                    }
                }
            }
        }

        Cell targetCell = null;

        if (potentialTargets.Any())
        {

            targetCell = potentialTargets[random.Next(potentialTargets.Count)];
        }
        else
        {

            var availableCells = playerBoard.Grid
                .Where(kvp => !kvp.Value.IsHit)
                .Select(kvp => kvp.Value)
                .ToList();

            if (!availableCells.Any())
            {
                throw new InvalidOperationException("No hay celdas disponibles para el ataque.");
            }

            targetCell = availableCells[random.Next(availableCells.Count)];
        }


        targetCell.IsHit = true;


        var actionDetails = $"El bot dispara en ({targetCell.X}, {targetCell.Y})";


        var hitShip = playerBoard.Ships
            .FirstOrDefault(s => s.Coordinates.Any(coord => coord.X == targetCell.X && coord.Y == targetCell.Y));

        if (hitShip != null)
        {

            actionDetails += hitShip.IsSunk ? " ¡Barco hundido!" : " ¡Acierto!";
        }
        else
        {
            actionDetails += " ¡Fallo!";
        }

        return new GameAction
        {
            PlayerId = -1,
            ActionType = "Shot",
            Timestamp = DateTime.UtcNow,
            Details = actionDetails
        };
    }



    public async Task<ServiceResponse<GameResponseDTO>> GetGameStateAsync(string userId, Guid gameId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null)
        {
            return new ServiceResponse<GameResponseDTO>
            {
                Success = false,
                Message = "La partida no existe."
            };
        }

        var participants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);

        var player1 = participants.FirstOrDefault(p => p.Role == ParticipantRole.Host);
        var player2 = participants.FirstOrDefault(p => p.Role == ParticipantRole.Guest);

        var player1Data = player1 != null
            ? await _userRepository.GetUserByIdAsync(player1.UserId)
            : null;

        var player2Data = player2 != null
            ? await _userRepository.GetUserByIdAsync(player2.UserId)
            : null;

        var response = new GameResponseDTO
        {
            GameId = game.GameId,
            Player1Nickname = player1Data?.Nickname ?? "Vacante",
            Player2Nickname = player2Data?.Nickname ?? "Vacante",
            Player1Role = player1?.Role.ToString() ?? "Vacante",
            Player2Role = player2?.Role.ToString() ?? "Vacante",
            StateDescription = GetStateDescription(game.State),
            Player1Board = DTOMapper.ToBoardDTO(game.Player1Board),
            Player2Board = DTOMapper.ToBoardDTO(game.Player2Board),
            Actions = game.Actions.Select(DTOMapper.ToGameActionDTO).ToList(),
            CurrentPlayerId = game.CurrentPlayerId ?? 0,
            CreatedAt = game.CreatedAt
        };

        return new ServiceResponse<GameResponseDTO> { Success = true, Data = response };
    }


    public async Task<ServiceResponse<string>> HandleDisconnectionAsync(int playerId)
    {
        var participants = await _gameParticipantRepository.GetParticipantsByUserIdAsync(playerId);
        if (participants == null || !participants.Any())
            return new ServiceResponse<string> { Success = false, Message = "El jugador no está en ninguna partida activa." };

        foreach (var p in participants)
        {
            var game = await _gameRepository.GetByIdAsync(p.GameId);
            if (game == null || game.State == GameState.Finished)
                continue;

            await _gameParticipantRepository.RemoveAsync(p);

            var remainingParticipants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(p.GameId);
            if (remainingParticipants.Count == 1)
            {
                var winner = remainingParticipants.First();
                game.State = GameState.Finished;
                game.WinnerId = winner.UserId;
                await _gameRepository.UpdateAsync(game);

                await _webSocketService.NotifyUserAsync(
                    winner.UserId,
                    "GameOver",
                    "Has ganado la partida por desconexión de tu oponente."
                );
             
            }
            else if (!remainingParticipants.Any())
            {
                game.State = GameState.WaitingForPlayers;
                await _gameRepository.UpdateAsync(game);
            }
        }

        return new ServiceResponse<string> { Success = true, Message = "Desconexión manejada correctamente." };
    }



    private string GetStateDescription(GameState state)
    {
        return state switch
        {
            GameState.WaitingForPlayers => "Esperando jugadores.",
            GameState.WaitingForPlayer1Ships => "El anfitrión está colocando sus barcos.",
            GameState.WaitingForPlayer2Ships => "El invitado está colocando sus barcos.",
            GameState.WaitingForPlayer1Shot => "Esperando disparo del anfitrión.",
            GameState.WaitingForPlayer2Shot => "Esperando disparo del invitado.",
            GameState.InProgress => "La partida está en progreso.",
            GameState.Finished => "La partida ha terminado.",
            _ => "Estado desconocido."
        };
    }

    public async Task<ServiceResponse<Game>> FindRandomOpponentAsync(string userId)
    {
        int userIdInt = Convert.ToInt32(userId);

        var availableGames = await _gameRepository.GetAllAsync();
        var game = availableGames
            .FirstOrDefault(g => g.State == GameState.WaitingForPlayers
                                 && !g.Participants.Any(p => p.UserId == userIdInt));

        if (game == null)
            return new ServiceResponse<Game> { Success = false, Message = "No hay partidas disponibles." };

        var participant = new GameParticipant
        {
            GameId = game.GameId,
            UserId = userIdInt,
            Role = ParticipantRole.Guest,
            IsReady = false
        };

        await _gameParticipantRepository.AddAsync(participant);

        game.State = GameState.WaitingForPlayer1Ships;
        await _gameRepository.UpdateAsync(game);

        return new ServiceResponse<Game> { Success = true, Data = game };
    }


    public async Task<ServiceResponse<Game>> CreateBotGameAsync(string userId)
    {
        int userIdInt = Convert.ToInt32(userId);
        var newGame = new Game
        {
            State = GameState.WaitingForPlayer1Ships,
            CreatedAt = DateTime.Now
        };

        await _gameRepository.AddAsync(newGame);

        var hostParticipant = new GameParticipant
        {
            GameId = newGame.GameId,
            UserId = userIdInt,
            Role = ParticipantRole.Host,
            IsReady = false
        };
        await _gameParticipantRepository.AddAsync(hostParticipant);

        var botParticipant = new GameParticipant
        {
            GameId = newGame.GameId,
            UserId = -1,
            Role = ParticipantRole.Guest,
            IsReady = false
        };
        await _gameParticipantRepository.AddAsync(botParticipant);


        if (newGame.Player2Board == null)
        {
            newGame.Player2Board = new Board();
        }


        var botShips = GenerateRandomShips(newGame.Player2Board);
        foreach (var ship in botShips)
        {
            newGame.Player2Board.Ships.Add(ship);
        }

        botParticipant.IsReady = true;
        await _gameParticipantRepository.UpdateAsync(botParticipant);
        await _gameRepository.UpdateAsync(newGame);

        return new ServiceResponse<Game> { Success = true, Data = newGame };
    }


    private List<Ship> GenerateRandomShips(Board board)
    {
        var shipSizes = new int[] { 5, 4, 3, 3, 2 };
        var ships = new List<Ship>();
        var random = new Random();
        foreach (var size in shipSizes)
        {
            bool placed = false;
            while (!placed)
            {
                int x = random.Next(0, 10);
                int y = random.Next(0, 10);
                bool horizontal = random.NextDouble() > 0.5;
                if (horizontal)
                {
                    if (x + size > 10)
                        continue;
                    bool conflict = false;
                    for (int i = 0; i < size; i++)
                    {
                        if (board.Grid[(x + i, y)].HasShip)
                        {
                            conflict = true;
                            break;
                        }
                    }
                    if (conflict)
                        continue;
                    var ship = new Ship
                    {
                        Name = $"Barco-{size}",
                        Size = size,
                        Coordinates = new List<Coordinate>()
                    };
                    for (int i = 0; i < size; i++)
                    {
                        ship.Coordinates.Add(new Coordinate { X = x + i, Y = y, IsHit = false });
                        board.Grid[(x + i, y)].HasShip = true;
                    }
                    ships.Add(ship);
                    placed = true;
                }
                else
                {
                    if (y + size > 10)
                        continue;
                    bool conflict = false;
                    for (int i = 0; i < size; i++)
                    {
                        if (board.Grid[(x, y + i)].HasShip)
                        {
                            conflict = true;
                            break;
                        }
                    }
                    if (conflict)
                        continue;
                    var ship = new Ship
                    {
                        Name = $"Barco-{size}",
                        Size = size,
                        Coordinates = new List<Coordinate>()
                    };
                    for (int i = 0; i < size; i++)
                    {
                        ship.Coordinates.Add(new Coordinate { X = x, Y = y + i, IsHit = false });
                        board.Grid[(x, y + i)].HasShip = true;
                    }
                    ships.Add(ship);
                    placed = true;
                }
            }
        }
        return ships;
    }



    public async Task<ServiceResponse<string>> PassTurnAsync(Guid gameId, int playerId)
    {
        await _turnLock.WaitAsync();
        try
        {
            var game = await _gameRepository.GetByIdAsync(gameId);
            if (game == null)
                return new ServiceResponse<string> { Success = false, Message = "La partida no existe." };

            if (game.State != GameState.WaitingForPlayer1Shot && game.State != GameState.WaitingForPlayer2Shot)
                return new ServiceResponse<string> { Success = false, Message = "No se puede pasar el turno en esta etapa." };

            var participants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);
            var currentPlayer = participants.FirstOrDefault(p => p.UserId == playerId);
            var opponent = participants.FirstOrDefault(p => p.UserId != playerId);

            if (currentPlayer == null || opponent == null)
                return new ServiceResponse<string> { Success = false, Message = "No se encontraron los participantes." };

            if ((game.State == GameState.WaitingForPlayer1Shot && currentPlayer.Role != ParticipantRole.Host) ||
                (game.State == GameState.WaitingForPlayer2Shot && currentPlayer.Role != ParticipantRole.Guest))
            {
                return new ServiceResponse<string> { Success = false, Message = "No es tu turno." };
            }


            game.State = game.State == GameState.WaitingForPlayer1Shot ? GameState.WaitingForPlayer2Shot : GameState.WaitingForPlayer1Shot;
            await _gameRepository.UpdateAsync(game);

            int nextPlayerId = game.State == GameState.WaitingForPlayer1Shot
                ? participants.First(p => p.Role == ParticipantRole.Host).UserId
                : participants.First(p => p.Role == ParticipantRole.Guest).UserId;

            await _webSocketService.NotifyUserAsync(nextPlayerId, "YourTurn", "Es tu turno de atacar.");

            return new ServiceResponse<string> { Success = true, Message = "Turno pasado correctamente." };
        }
        finally
        {
            _turnLock.Release();
        }
    }


    public async Task<ServiceResponse<string>> ReassignRolesAsync(Guid gameId)
    {
        var participants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);
        if (participants.Count < 2)
            return new ServiceResponse<string> { Success = false, Message = "No hay suficientes jugadores para reasignar roles." };

        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null)
            return new ServiceResponse<string> { Success = false, Message = "La partida no existe." };

        var host = participants.FirstOrDefault(p => p.Role == ParticipantRole.Host);
        var guest = participants.FirstOrDefault(p => p.Role == ParticipantRole.Guest);

        if (host == null && guest != null)
        {
            guest.Role = ParticipantRole.Host;
            await _gameParticipantRepository.UpdateAsync(guest);
        }

        return new ServiceResponse<string> { Success = true, Message = "Roles reasignados correctamente." };
    }

    public async Task<ServiceResponse<Game>> RematchAsync(Guid gameId, int playerId)
    {
        var oldGame = await _gameRepository.GetByIdAsync(gameId);
        if (oldGame == null)
            return new ServiceResponse<Game> { Success = false, Message = "Juego no encontrado." };
        var participants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);
        if (participants.Count < 2)
            return new ServiceResponse<Game> { Success = false, Message = "No hay suficientes jugadores para revancha." };
        var newGame = new Game { State = GameState.WaitingForPlayer1Ships, CreatedAt = DateTime.Now };
        await _gameRepository.AddAsync(newGame);
        foreach (var participant in participants)
        {
            var newParticipant = new GameParticipant { GameId = newGame.GameId, UserId = participant.UserId, Role = participant.Role, IsReady = false };
            await _gameParticipantRepository.AddAsync(newParticipant);
        }
        return new ServiceResponse<Game> { Success = true, Data = newGame };
    }


    private async Task DeleteFinishedGame(Game game)
    {
        if (game.State == GameState.Finished)
        {

            await _gameRepository.RemoveAsync(game);

        }
    }

}
