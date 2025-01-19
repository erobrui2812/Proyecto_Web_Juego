﻿
using hundir_la_flota.Models;

public interface IGameService
{
    Task<ServiceResponse<Game>> CreateGameAsync(string userId);
    Task<ServiceResponse<Game>> GetGameStateAsync(string userId, Guid gameId);
    Task<ServiceResponse<string>> JoinGameAsync(Guid gameId, int playerId);
    Task<ServiceResponse<string>> PlaceShipsAsync(Guid gameId, int playerId, List<Ship> ships);
    Task<ServiceResponse<string>> AttackAsync(Guid gameId, int playerId, int x, int y);
    Task<ServiceResponse<Game>> FindRandomOpponentAsync(string userId);
    Task<ServiceResponse<Game>> CreateBotGameAsync(string userId);
}

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;

    public GameService(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<ServiceResponse<Game>> CreateGameAsync(string userId)
    {
        int userIdInt = Convert.ToInt32(userId);
        var newGame = new Game
        {
            Player1Id = userIdInt,
            CurrentPlayerId = userIdInt,
            State = GameState.WaitingForPlayer1Ships
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
        game.State = GameState.WaitingForPlayer1Ships;

        await _gameRepository.UpdateAsync(game);

        return new ServiceResponse<string> { Success = true, Message = "Te has unido a la partida." };
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
                board.Grid[coord.X, coord.Y].HasShip = true;

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
        var cell = opponentBoard.Grid[x, y];

        if (cell.IsHit)
            return new ServiceResponse<string> { Success = false, Message = "Ya has atacado esta celda." };

        cell.IsHit = true;

        var ship = opponentBoard.Ships.FirstOrDefault(s => s.Coordinates.Any(coord => coord.X == x && coord.Y == y));
        if (ship != null && ship.IsSunk)
        {
            if (opponentBoard.Ships.All(s => s.IsSunk))
            {
                game.State = GameState.Finished;
                await _gameRepository.UpdateAsync(game);
                return new ServiceResponse<string> { Success = true, Message = "¡Ganaste la partida!" };
            }

            return new ServiceResponse<string> { Success = true, Message = "¡Barco hundido!" };
        }

        game.CurrentPlayerId = playerId == game.Player1Id ? game.Player2Id : game.Player1Id;

        await _gameRepository.UpdateAsync(game);

        return new ServiceResponse<string> { Success = true, Message = "Disparo realizado." };
    }

    public async Task<ServiceResponse<Game>> GetGameStateAsync(string userId, Guid gameId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);

        if (game == null)
            return new ServiceResponse<Game> { Success = false, Message = "La partida no existe." };

        return new ServiceResponse<Game> { Success = true, Data = game };
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
            Player2Id = -1, // Representa al bot
            CurrentPlayerId = userIdInt,
            State = GameState.WaitingForPlayer1Ships
        };

        await _gameRepository.AddAsync(newGame);

        return new ServiceResponse<Game> { Success = true, Data = newGame };
    }
}
