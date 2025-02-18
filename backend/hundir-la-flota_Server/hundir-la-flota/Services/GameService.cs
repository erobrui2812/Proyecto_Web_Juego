using hundir_la_flota.DTOs;
using hundir_la_flota.Models;
using hundir_la_flota.Repositories;
using hundir_la_flota.Services;
using hundir_la_flota.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    Task<ServiceResponse<string>> ConfirmReadyAsync(Guid gameId, int playerId);
    Task<int?> GetHostIdAsync(Guid gameId);
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

    public async Task<ServiceResponse<string>> ConfirmReadyAsync(Guid gameId, int playerId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null)
            return new ServiceResponse<string> { Success = false, Message = "La partida no existe." };

        var participants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);
        var participant = participants.FirstOrDefault(p => p.UserId == playerId);
        if (participant == null)
            return new ServiceResponse<string> { Success = false, Message = "Participante no válido." };

        participant.IsReady = true;
        await _gameParticipantRepository.UpdateAsync(participant);

        participants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);

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

        return new ServiceResponse<string> { Success = true, Message = "Confirm ready successfully." };
    }

    public async Task<ServiceResponse<Game>> CreateGameAsync(string userId)
    {
        int userIdInt = Convert.ToInt32(userId);

        var newGame = new Game
        {
            State = GameState.WaitingForPlayers,
            CreatedAt = DateTime.Now,
            Player1Board = new Board(),
            Player2Board = new Board()
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

    public async Task<ServiceResponse<Game>> FindRandomOpponentAsync(string userId)
    {
        int userIdInt = Convert.ToInt32(userId);
        var availableGames = await _gameRepository.GetAllAsync();
        var game = availableGames.FirstOrDefault(g =>
            g.State == GameState.WaitingForPlayers &&
            !g.Participants.Any(p => p.UserId == userIdInt)
        );
        if (game == null)
            return new ServiceResponse<Game> { Success = false, Message = "No hay partidas disponibles." };

        if (game.Player1Board == null) game.Player1Board = new Board();
        if (game.Player2Board == null) game.Player2Board = new Board();

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

    public async Task<int?> GetHostIdAsync(Guid gameId)
    {
        var participants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);
        var host = participants.FirstOrDefault(p => p.Role == ParticipantRole.Host);
        return host?.UserId;
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
        var participants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);
        if (participants == null || !participants.Any(p => p.UserId == userId))
            return new ServiceResponse<string> { Success = false, Message = "No estás participando en esta partida." };

        var participant = participants.First(p => p.UserId == userId);
        participant.Abandoned = true;
        await _gameParticipantRepository.UpdateAsync(participant);

        var activeParticipants = participants.Where(p => !p.Abandoned).ToList();
        var game = await _gameRepository.GetByIdAsync(gameId);

        if (!activeParticipants.Any())
        {
            game.State = GameState.WaitingForPlayers;
        }
        else if (activeParticipants.Count == 1)
        {
            game.State = GameState.Finished;
            game.WinnerId = activeParticipants.First().UserId;
            await _webSocketService.NotifyUserAsync(
                activeParticipants.First().UserId,
                "GameOver",
                $"El jugador {activeParticipants.First().UserId} ha ganado por abandono del oponente."
            );
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

            bool isPlayer1Turn = (game.State == GameState.WaitingForPlayer1Shot && currentPlayer.Role == ParticipantRole.Host);
            bool isPlayer2Turn = (game.State == GameState.WaitingForPlayer2Shot && currentPlayer.Role == ParticipantRole.Guest);
            if (!isPlayer1Turn && !isPlayer2Turn)
                return new ServiceResponse<string> { Success = false, Message = "No es tu turno." };

            var opponentBoard = (currentPlayer.Role == ParticipantRole.Host)
                ? game.Player2Board
                : game.Player1Board;
            if (!opponentBoard.Grid.ContainsKey((x, y)))
                return new ServiceResponse<string> { Success = false, Message = "Coordenada fuera de los límites." };

            var cell = opponentBoard.Grid[(x, y)];
            if (cell.IsHit)
                return new ServiceResponse<string> { Success = false, Message = "Celda ya atacada." };

            bool wasHit = cell.HasShip;
            cell.IsHit = true;
            cell.Status = wasHit ? CellStatus.Hit : CellStatus.Miss;

            string attackResult = wasHit ? "hit" : "miss";
            string actionDetails = $"Disparo en ({x}, {y}). " + (wasHit ? "¡Acierto!" : "Fallo.");

            var hitShip = opponentBoard.Ships.FirstOrDefault(s =>
                s.Coordinates.Any(coord => coord.X == x && coord.Y == y)
            );
            if (hitShip != null)
            {
                var coordHit = hitShip.Coordinates.First(c => c.X == x && c.Y == y);
                coordHit.IsHit = true;
                if (hitShip.IsSunk)
                {
                    attackResult = "sunk";
                    actionDetails += " Barco hundido!";
                }
            }

            if (opponentBoard.AreAllShipsSunk())
            {
                game.State = GameState.Finished;
                game.WinnerId = playerId;
                actionDetails += " ¡Fin del juego!";
                await _webSocketService.NotifyUsersAsync(
                    new List<int> { playerId, opponent.UserId },
                    "GameOver",
                    $"{playerId}"
                );
                await UpdatePlayerStats(playerId, opponent.UserId);
            }
            else
            {
                if (!wasHit)
                {
                    game.State = (game.State == GameState.WaitingForPlayer1Shot)
                        ? GameState.WaitingForPlayer2Shot
                        : GameState.WaitingForPlayer1Shot;
                }
            }
            await _gameRepository.UpdateAsync(game);

            var payload = JsonConvert.SerializeObject(new { x, y, result = attackResult });
            await _webSocketService.NotifyUserAsync(playerId, "AttackResult", payload);
            await _webSocketService.NotifyUserAsync(opponent.UserId, "EnemyAttack", payload);

            if (opponent.UserId == -1 && game.State != GameState.Finished)
            {
                var botParticipant = participants.FirstOrDefault(p => p.UserId == -1);
                var humanParticipant = participants.FirstOrDefault(p => p.UserId != -1);

                bool isBotTurn =
                    (game.State == GameState.WaitingForPlayer2Shot && botParticipant.Role == ParticipantRole.Guest)
                    || (game.State == GameState.WaitingForPlayer1Shot && botParticipant.Role == ParticipantRole.Host);

                while (isBotTurn && game.State != GameState.Finished)
                {
                    var botAction = SimulateBotAttack(
                        (botParticipant.Role == ParticipantRole.Host) ? game.Player2Board : game.Player1Board
                    );
                    var botShotResult = ResolveBotShot(game, botParticipant, humanParticipant, botAction.X, botAction.Y);

                    var botPayload = JsonConvert.SerializeObject(new { x = botAction.X, y = botAction.Y, result = botShotResult.Result });
                    await _webSocketService.NotifyUserAsync(humanParticipant.UserId, "EnemyAttack", botPayload);

                    if (game.State == GameState.Finished) break;

                    if (botShotResult.Result == "miss")
                    {
                        game.State = (botParticipant.Role == ParticipantRole.Host)
                            ? GameState.WaitingForPlayer2Shot
                            : GameState.WaitingForPlayer1Shot;
                        await _gameRepository.UpdateAsync(game);

                        await _webSocketService.NotifyUserAsync(
                            humanParticipant.UserId,
                            "YourTurn",
                            "Es tu turno de atacar."
                        );
                        break;
                    }
                    else
                    {
                        isBotTurn =
                            (game.State == GameState.WaitingForPlayer2Shot && botParticipant.Role == ParticipantRole.Guest)
                            || (game.State == GameState.WaitingForPlayer1Shot && botParticipant.Role == ParticipantRole.Host);
                    }
                }
            }
            else
            {
                if (game.State != GameState.Finished)
                {
                    if (!wasHit)
                    {
                        int nextPlayerId = (game.State == GameState.WaitingForPlayer1Shot)
                            ? participants.First(p => p.Role == ParticipantRole.Host).UserId
                            : participants.First(p => p.Role == ParticipantRole.Guest).UserId;
                        await _webSocketService.NotifyUserAsync(nextPlayerId, "YourTurn", "Es tu turno de atacar.");
                    }
                    else
                    {
                        await _webSocketService.NotifyUserAsync(playerId, "YourTurn", "Continúa atacando (acertaste).");
                    }
                }
            }

            return new ServiceResponse<string> { Success = true, Message = actionDetails };
        }
        finally
        {
            _turnLock.Release();
        }
    }

    private (bool Success, string Result) ResolveBotShot(
        Game game,
        GameParticipant botParticipant,
        GameParticipant humanParticipant,
        int x,
        int y)
    {
        var targetBoard = (botParticipant.Role == ParticipantRole.Host)
            ? game.Player2Board
            : game.Player1Board;

        if (!targetBoard.Grid.ContainsKey((x, y)))
            return (false, "miss");

        var cell = targetBoard.Grid[(x, y)];
        if (cell.IsHit)
            return (false, "alreadyHit");

        cell.IsHit = true;
        cell.Status = cell.HasShip ? CellStatus.Hit : CellStatus.Miss;

        string result = cell.HasShip ? "hit" : "miss";
        if (cell.HasShip)
        {
            var ship = targetBoard.Ships.FirstOrDefault(s =>
                s.Coordinates.Any(coord => coord.X == x && coord.Y == y)
            );
            if (ship != null)
            {
                var coordHit = ship.Coordinates.First(c => c.X == x && c.Y == y);
                coordHit.IsHit = true;
                if (ship.IsSunk)
                    result = "sunk";
            }

            if (targetBoard.AreAllShipsSunk())
            {
                game.State = GameState.Finished;
                game.WinnerId = botParticipant.UserId;
                _ = _webSocketService.NotifyUserAsync(
                    humanParticipant.UserId,
                    "GameOver",
                    "El bot ha ganado. ¡Fin de la partida!"
                );
                _ = UpdatePlayerStats(botParticipant.UserId, humanParticipant.UserId);
            }
        }
        _ = _gameRepository.UpdateAsync(game);
        return (true, result);
    }

    public GameAction SimulateBotAttack(Board boardToAttack)
    {
        var random = new Random();
        var potentialTargets = new List<Cell>();

        foreach (var ship in boardToAttack.Ships)
        {
            if (ship.IsSunk) continue;
            foreach (var coord in ship.Coordinates)
            {
                var cell = boardToAttack.Grid[(coord.X, coord.Y)];
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
                        if (adjacent.X >= 0 && adjacent.X < Board.Size
                            && adjacent.Y >= 0 && adjacent.Y < Board.Size)
                        {
                            var adjacentCell = boardToAttack.Grid[(adjacent.X, adjacent.Y)];
                            if (!adjacentCell.IsHit)
                                potentialTargets.Add(adjacentCell);
                        }
                    }
                }
            }
        }

        Cell targetCell;
        if (potentialTargets.Any())
        {
            targetCell = potentialTargets[random.Next(potentialTargets.Count)];
        }
        else
        {
            var availableCells = boardToAttack.Grid
                .Where(kvp => !kvp.Value.IsHit)
                .Select(kvp => kvp.Value)
                .ToList();
            if (!availableCells.Any())
                throw new InvalidOperationException("No hay celdas disponibles para el ataque.");

            targetCell = availableCells[random.Next(availableCells.Count)];
        }

        var details = $"El bot dispara en ({targetCell.X}, {targetCell.Y})";

        return new GameAction
        {
            PlayerId = -1,
            ActionType = "Shot",
            Timestamp = DateTime.UtcNow,
            Details = details,
            X = targetCell.X,
            Y = targetCell.Y
        };
    }

    public async Task<ServiceResponse<Game>> CreateBotGameAsync(string userId)
    {
        int userIdInt = Convert.ToInt32(userId);
        var newGame = new Game
        {
            State = GameState.WaitingForPlayer1Ships,
            CreatedAt = DateTime.Now,
            Player1Board = new Board(),
            Player2Board = new Board()
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

        var botShips = GenerateRandomShips(newGame.Player2Board);
        foreach (var ship in botShips)
            newGame.Player2Board.Ships.Add(ship);

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
                int x = random.Next(0, Board.Size);
                int y = random.Next(0, Board.Size);
                bool horizontal = random.NextDouble() > 0.5;
                if (horizontal)
                {
                    if (x + size > Board.Size) continue;
                    bool conflict = false;
                    for (int i = 0; i < size; i++)
                    {
                        if (!board.Grid.ContainsKey((x + i, y)) || board.Grid[(x + i, y)].HasShip)
                        {
                            conflict = true;
                            break;
                        }
                    }
                    if (conflict) continue;
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
                    if (y + size > Board.Size) continue;
                    bool conflict = false;
                    for (int i = 0; i < size; i++)
                    {
                        if (board.Grid[(x, y + i)].HasShip)
                        {
                            conflict = true;
                            break;
                        }
                    }
                    if (conflict) continue;
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

            p.Abandoned = true;
            await _gameParticipantRepository.UpdateAsync(p);

            var remainingParticipants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(p.GameId);
            var activeParticipants = remainingParticipants.Where(x => !x.Abandoned).ToList();

            if (activeParticipants.Count == 1)
            {
                var winner = activeParticipants.First();
                game.State = GameState.Finished;
                game.WinnerId = winner.UserId;
                await _gameRepository.UpdateAsync(game);
                await _webSocketService.NotifyUserAsync(
                    winner.UserId,
                    "GameOver",
                    "Has ganado la partida por desconexión de tu oponente."
                );
            }
            else if (!activeParticipants.Any())
            {
                game.State = GameState.WaitingForPlayers;
                await _gameRepository.UpdateAsync(game);
            }
        }
        return new ServiceResponse<string> { Success = true, Message = "Desconexión manejada correctamente." };
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

            if (currentPlayer == null)
                return new ServiceResponse<string> { Success = false, Message = "No se encontró al participante." };

            if ((game.State == GameState.WaitingForPlayer1Shot && currentPlayer.Role != ParticipantRole.Host) ||
                (game.State == GameState.WaitingForPlayer2Shot && currentPlayer.Role != ParticipantRole.Guest))
            {
                return new ServiceResponse<string> { Success = false, Message = "No es tu turno." };
            }

            game.State = (game.State == GameState.WaitingForPlayer1Shot)
                ? GameState.WaitingForPlayer2Shot
                : GameState.WaitingForPlayer1Shot;

            await _gameRepository.UpdateAsync(game);

            var nextPlayer = participants.FirstOrDefault(p =>
                (game.State == GameState.WaitingForPlayer1Shot && p.Role == ParticipantRole.Host) ||
                (game.State == GameState.WaitingForPlayer2Shot && p.Role == ParticipantRole.Guest)
            );
            if (nextPlayer != null)
            {
                await _webSocketService.NotifyUserAsync(nextPlayer.UserId, "YourTurn", "Es tu turno de atacar.");
            }

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

        if (oldGame.RematchRequestedAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - oldGame.RematchRequestedAt.Value;
            if (elapsed > TimeSpan.FromSeconds(30))
            {
                oldGame.RematchRequests = new List<int>();
                oldGame.RematchRequestedAt = null;
                await _gameRepository.UpdateAsync(oldGame);
                return new ServiceResponse<Game> { Success = false, Message = "La solicitud de revancha expiró." };
            }
        }

        if (oldGame.RematchRequests == null || oldGame.RematchRequests.Count == 0)
        {
            oldGame.RematchRequests = new List<int> { playerId };
            oldGame.RematchRequestedAt = DateTime.UtcNow;
            await _gameRepository.UpdateAsync(oldGame);

            foreach (var participant in participants)
            {
                if (participant.UserId != playerId)
                {
                    await _webSocketService.NotifyUserAsync(
                        participant.UserId,
                        "RematchRequested",
                        "El oponente ha solicitado revancha. Tienes 30 segundos para aceptarla."
                    );
                }
            }
            return new ServiceResponse<Game> { Success = true, Message = "Revancha solicitada. Esperando al oponente." };
        }

        if (!oldGame.RematchRequests.Contains(playerId))
        {
            oldGame.RematchRequests.Add(playerId);
            await _gameRepository.UpdateAsync(oldGame);
        }

        if (oldGame.RematchRequests.Count == 2)
        {
            var newGame = new Game
            {
                State = GameState.WaitingForPlayer1Ships,
                CreatedAt = DateTime.Now,
                Player1Board = new Board(),
                Player2Board = new Board()
            };
            await _gameRepository.AddAsync(newGame);

            foreach (var participant in participants)
            {
                var newParticipant = new GameParticipant
                {
                    GameId = newGame.GameId,
                    UserId = participant.UserId,
                    Role = participant.Role,
                    IsReady = false
                };
                await _gameParticipantRepository.AddAsync(newParticipant);
            }

            foreach (var participant in participants)
            {
                await _webSocketService.NotifyUserAsync(
                    participant.UserId,
                    "RematchCreated",
                    newGame.GameId.ToString()
                );
            }
            return new ServiceResponse<Game> { Success = true, Data = newGame };
        }

        return new ServiceResponse<Game> { Success = true, Message = "Revancha solicitada. Esperando al oponente." };
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
        var player1Data = player1 != null ? await _userRepository.GetUserByIdAsync(player1.UserId) : null;
        var player2Data = player2 != null ? await _userRepository.GetUserByIdAsync(player2.UserId) : null;

        var player1Nickname = player1 != null
            ? (player1.Abandoned ? "Abandonado" : (player1Data?.Nickname ?? "Vacante"))
            : "Vacante";
        var player2Nickname = player2 != null
            ? (player2.Abandoned ? "Abandonado" : (player2Data?.Nickname ?? "Vacante"))
            : "Vacante";

        var response = new GameResponseDTO
        {
            GameId = game.GameId,
            Player1Nickname = player1Nickname,
            Player2Nickname = player2Nickname,
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
}
