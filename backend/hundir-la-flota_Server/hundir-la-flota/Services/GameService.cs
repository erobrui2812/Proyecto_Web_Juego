using hundir_la_flota.DTOs;
using hundir_la_flota.Models;
using hundir_la_flota.Repositories;
using hundir_la_flota.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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
}

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWebSocketService _webSocketService;

    public GameService(IGameRepository gameRepository, IUserRepository userRepository, IWebSocketService webSocketService)
    {
        _gameRepository = gameRepository;
        _userRepository = userRepository;
        _webSocketService = webSocketService;
    }

    public async Task<ServiceResponse<Game>> CreateGameAsync(string userId)
    {
        int userIdInt = Convert.ToInt32(userId);
        var newGame = new Game
        {
            Player1Id = userIdInt,
            Player1Role = "Anfitrión",
            Player2Role = "Vacante",
            CurrentPlayerId = userIdInt,
            State = GameState.WaitingForPlayers
        };

        await _gameRepository.AddAsync(newGame);
        return new ServiceResponse<Game> { Success = true, Data = newGame };
    }

    public async Task<ServiceResponse<string>> JoinGameAsync(Guid gameId, int playerId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null || game.State != GameState.WaitingForPlayers)
            return new ServiceResponse<string> { Success = false, Message = "La partida no está disponible." };

        game.Player2Id = playerId;
        game.Player2Role = "Invitado";
        game.State = GameState.WaitingForPlayer1Ships;

        await _gameRepository.UpdateAsync(game);
        return new ServiceResponse<string> { Success = true, Message = "Te has unido a la partida." };
    }

    public async Task<ServiceResponse<string>> AbandonGameAsync(Guid gameId, int playerId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null)
            return new ServiceResponse<string> { Success = false, Message = "La partida no existe." };

        if (game.Player1Id == playerId)
        {
            game.Player1Id = -1;
            game.Player1Role = "Vacante";
        }
        else if (game.Player2Id == playerId)
        {
            game.Player2Id = -1;
            game.Player2Role = "Vacante";
        }

        if (game.Player1Id == -1 && game.Player2Id == -1)
            game.State = GameState.WaitingForPlayers;

        await _gameRepository.UpdateAsync(game);
        return new ServiceResponse<string> { Success = true, Message = "Jugador ha abandonado la partida." };
    }

    public async Task<ServiceResponse<string>> PlaceShipsAsync(Guid gameId, int playerId, List<Ship> ships)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null || game.State == GameState.Finished)
            return new ServiceResponse<string> { Success = false, Message = "No se puede colocar barcos en esta etapa." };

        var board = playerId == game.Player1Id ? game.Player1Board : game.Player2Board;
        foreach (var ship in ships)
        {
            if (!board.IsShipPlacementValid(ship))
                return new ServiceResponse<string> { Success = false, Message = "Las posiciones de los barcos no son válidas." };

            foreach (var coord in ship.Coordinates)
            {
                var cell = board.Grid.FirstOrDefault(c => c.X == coord.X && c.Y == coord.Y);
                if (cell != null) cell.HasShip = true;
            }
            board.Ships.Add(ship);
        }

        if (game.Player1Board.Ships.Count > 0 && game.Player2Board.Ships.Count > 0)
            game.State = GameState.WaitingForPlayer1Shot;

        await _gameRepository.UpdateAsync(game);
        return new ServiceResponse<string> { Success = true, Message = "Barcos colocados correctamente." };
    }

    public async Task<ServiceResponse<string>> AttackAsync(Guid gameId, int playerId, int x, int y)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null || game.State != GameState.WaitingForPlayer1Shot && game.State != GameState.WaitingForPlayer2Shot)
            return new ServiceResponse<string> { Success = false, Message = "No se puede atacar en esta etapa." };

        if (playerId != game.CurrentPlayerId)
            return new ServiceResponse<string> { Success = false, Message = "No es tu turno." };

        var opponentBoard = playerId == game.Player1Id ? game.Player2Board : game.Player1Board;
        var cell = opponentBoard.Grid.FirstOrDefault(c => c.X == x && c.Y == y);

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
            }
        }
        else
        {
            actionDetails += " ¡Fallo!";
        }

        game.Actions.Add(new GameAction
        {
            PlayerId = playerId,
            ActionType = "Shot",
            Timestamp = DateTime.UtcNow,
            Details = actionDetails
        });

        if (game.State != GameState.Finished)
        {
            game.CurrentPlayerId = playerId == game.Player1Id ? game.Player2Id : game.Player1Id;

          
            if (game.Player2Id == -1 && game.CurrentPlayerId == game.Player2Id)
            {
                var botAction = SimulateBotAttack(game.Player1Board);
                game.Actions.Add(botAction);

             
                if (game.Player1Board.Ships.All(s => s.IsSunk))
                {
                    game.State = GameState.Finished;
                    game.WinnerId = game.Player2Id;
                    botAction.Details += " Fin del juego.";
                }
                else
                {
                    game.CurrentPlayerId = game.Player1Id; 
                }
            }
        }

        await _gameRepository.UpdateAsync(game);
        return new ServiceResponse<string> { Success = true, Message = actionDetails };
    }

 
    private GameAction SimulateBotAttack(Board playerBoard)
    {
        var availableCells = playerBoard.Grid.Where(c => !c.IsHit).ToList();
        var random = new Random();
        var targetCell = availableCells[random.Next(availableCells.Count)];

        targetCell.IsHit = true;

        var actionDetails = $"El bot dispara en ({targetCell.X}, {targetCell.Y})";

        var ship = playerBoard.Ships.FirstOrDefault(s => s.Coordinates.Any(coord => coord.X == targetCell.X && coord.Y == targetCell.Y));
        if (ship != null)
        {
            actionDetails += ship.IsSunk ? " ¡Barco hundido!" : " ¡Acierto!";
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
            return new ServiceResponse<GameResponseDTO> { Success = false, Message = "La partida no existe." };

        var player1 = game.Player1Id > 0 ? await _userRepository.GetUserByIdAsync(game.Player1Id) : null;
        var player2 = game.Player2Id > 0 ? await _userRepository.GetUserByIdAsync(game.Player2Id) : null;

        var response = new GameResponseDTO
        {
            GameId = game.GameId,
            Player1Nickname = player1?.Nickname ?? "Vacante",
            Player2Nickname = player2?.Nickname ?? "Vacante",
            Player1Display = $"{player1?.Nickname ?? "Vacante"} - {game.Player1Role}",
            Player2Display = $"{player2?.Nickname ?? "Vacante"} - {game.Player2Role}",
            Player1Role = game.Player1Role,
            Player2Role = game.Player2Role,
            StateDescription = GetStateDescription(game.State),
            Player1Board = game.Player1Board,
            Player2Board = game.Player2Board,
            Actions = game.Actions.ToList(),
            CurrentPlayerId = game.CurrentPlayerId,
            CreatedAt = game.CreatedAt
        };

        return new ServiceResponse<GameResponseDTO> { Success = true, Data = response };
    }


    public async Task<ServiceResponse<string>> HandleDisconnectionAsync(int playerId)
    {
        var games = await _gameRepository.GetGamesByPlayerIdAsync(playerId);
        foreach (var game in games)
        {
            if (game.State == GameState.Finished) continue;

            if (game.Player1Id == playerId)
            {
                game.Player1Id = -1;
                game.Player1Role = "Vacante";
                if (game.State == GameState.InProgress || game.State == GameState.WaitingForPlayer1Shot)
                {
                    game.WinnerId = game.Player2Id;
                    game.State = GameState.Finished;
                    await _webSocketService.NotifyUserAsync(game.Player2Id, "GameWon", "Ganaste automáticamente porque tu oponente se desconectó.");
                }
            }
            else if (game.Player2Id == playerId)
            {
                game.Player2Id = -1;
                game.Player2Role = "Vacante";
                if (game.State == GameState.InProgress || game.State == GameState.WaitingForPlayer2Shot)
                {
                    game.WinnerId = game.Player1Id;
                    game.State = GameState.Finished;
                    await _webSocketService.NotifyUserAsync(game.Player1Id, "GameWon", "Ganaste automáticamente porque tu oponente se desconectó.");
                }
            }

            if (game.Player1Id == -1 && game.Player2Id == -1)
                game.State = GameState.WaitingForPlayers;

            await _gameRepository.UpdateAsync(game);
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

        var games = await _gameRepository.GetAllAsync();
        var availableGame = games.FirstOrDefault(g => g.State == GameState.WaitingForPlayers && g.Player1Id != userIdInt);

        if (availableGame == null)
            return new ServiceResponse<Game> { Success = false, Message = "No hay partidas disponibles." };

        availableGame.Player2Id = userIdInt;
        availableGame.State = GameState.WaitingForPlayer1Ships;

        await _gameRepository.UpdateAsync(availableGame);
        return new ServiceResponse<Game> { Success = true, Data = availableGame };
    }

    public async Task<ServiceResponse<Game>> CreateBotGameAsync(string userId)
    {
        int userIdInt = Convert.ToInt32(userId);

        var newGame = new Game
        {
            Player1Id = userIdInt,
            Player2Id = -1,
            CurrentPlayerId = userIdInt,
            State = GameState.WaitingForPlayer1Ships
        };

        await _gameRepository.AddAsync(newGame);
        return new ServiceResponse<Game> { Success = true, Data = newGame };
    }

    public async Task<ServiceResponse<string>> ReassignRolesAsync(Guid gameId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null)
            return new ServiceResponse<string> { Success = false, Message = "La partida no existe." };

        if (game.Player1Id == -1 && game.Player2Id != -1)
        {
            game.Player1Id = game.Player2Id;
            game.Player2Id = -1;
        }
        else if (game.Player2Id == -1 && game.Player1Id != -1)
        {
            game.Player2Id = game.Player1Id;
            game.Player1Id = -1;
        }

        await _gameRepository.UpdateAsync(game);
        return new ServiceResponse<string> { Success = true, Message = "Roles reasignados correctamente." };
    }
}
