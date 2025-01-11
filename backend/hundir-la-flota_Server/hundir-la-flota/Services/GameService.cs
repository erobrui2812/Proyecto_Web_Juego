using hundir_la_flota.Models;
using hundir_la_flota.Repositories;

public interface IGameService
{
    Task<ServiceResponse<Game>> CreateGameAsync(string userId);
    Task<ServiceResponse<Game>> GetGameStateAsync(string userId, Guid gameId);
    Task<ServiceResponse<string>> JoinGameAsync(Guid gameId, int playerId);
    Task<ServiceResponse<string>> PlaceShipsAsync(Guid gameId, int playerId, List<Ship> ships);
    Task<ServiceResponse<string>> AttackAsync(Guid gameId, int playerId, int x, int y);
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
        var newGame = new Game
        {
            Player1Id = Convert.ToInt32(userId), // Convertir userId a int
            CurrentPlayerId = Convert.ToInt32(userId) // El primer jugador comienza
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

        board.Ships = ships;

        // Validar posiciones de los barcos
        if (!AreShipsValid(board))
            return new ServiceResponse<string> { Success = false, Message = "Las posiciones de los barcos no son válidas." };

        // Cambiar el estado según los barcos colocados
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

    private bool AreShipsValid(Board board)
    {
        // Implementar validación de la colocación de los barcos
        return true;
    }
}
