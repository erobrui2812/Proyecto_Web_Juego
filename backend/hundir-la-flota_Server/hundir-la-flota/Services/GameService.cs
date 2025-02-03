using hundir_la_flota.DTOs;
using hundir_la_flota.Models;
using hundir_la_flota.Repositories;
using hundir_la_flota.Services;
using hundir_la_flota.Utils;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Tokens;

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
}

public class GameService : IGameService
{
    private static readonly SemaphoreSlim _turnLock = new(1, 1);
    private readonly IGameRepository _gameRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWebSocketService _webSocketService;
    private readonly IGameParticipantRepository _gameParticipantRepository;


    public GameService(
     IGameRepository gameRepository,
     IUserRepository userRepository,
     IGameParticipantRepository gameParticipantRepository,
     IWebSocketService webSocketService)
    {
        _gameRepository = gameRepository;
        _userRepository = userRepository;
        _gameParticipantRepository = gameParticipantRepository;
        _webSocketService = webSocketService;
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
        if (game == null || game.State != GameState.WaitingForPlayers)
            return new ServiceResponse<string> { Success = false, Message = "La partida no está disponible." };

        var existingParticipant = await _gameParticipantRepository.GetParticipantsByGameIdAsync(gameId);
        if (existingParticipant.Any(p => p.UserId == userId))
            return new ServiceResponse<string> { Success = false, Message = "Ya estás en esta partida." };

        var participant = new GameParticipant
        {
            GameId = gameId,
            UserId = userId,
            Role = ParticipantRole.Guest,
            IsReady = false
        };

        await _gameParticipantRepository.AddAsync(participant);

        if (existingParticipant.Count + 1 >= 2)
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

            // Marcar la celda como atacada
            cell.IsHit = true;

            // Verificar si se ha impactado un barco
            var ship = opponentBoard.Ships.FirstOrDefault(s => s.Coordinates.Any(coord => coord.X == x && coord.Y == y));
            string resultStatus = "miss";
            if (ship != null)
            {
                // Aquí podrías actualizar el estado del barco (por ejemplo, marcando la parte impactada)
                // y determinar si está hundido.
                resultStatus = ship.IsSunk ? "sunk" : "hit";
            }

            // Si todos los barcos del oponente están hundidos, finalizar el juego
            if (ship != null && opponentBoard.Ships.All(s => s.IsSunk))
            {
                game.State = GameState.Finished;
                game.WinnerId = playerId;
                var gameOverPayload = new
                {
                    winner = playerId,
                    message = $"El jugador {playerId} ha ganado."
                };
                await _webSocketService.NotifyUsersAsync(
                    new List<int> { playerId, opponent.UserId },
                    "GameOver",
                    JsonConvert.SerializeObject(gameOverPayload)
                );
            }
            else
            {
                // Enviar el resultado del ataque en formato JSON al oponente
                var attackResponse = new { x = x, y = y, result = resultStatus };
                string jsonResponse = JsonConvert.SerializeObject(attackResponse);
                await _webSocketService.NotifyUserAsync(opponent.UserId, "AttackResult", jsonResponse);
            }

            // Si el juego sigue, cambiar turno y notificar al siguiente jugador
            if (game.State != GameState.Finished)
            {
                game.State = game.State == GameState.WaitingForPlayer1Shot
                    ? GameState.WaitingForPlayer2Shot
                    : GameState.WaitingForPlayer1Shot;
                await _gameRepository.UpdateAsync(game);

                int nextPlayerId = game.State == GameState.WaitingForPlayer1Shot
                    ? participants.First(p => p.Role == ParticipantRole.Host).UserId
                    : participants.First(p => p.Role == ParticipantRole.Guest).UserId;

                await _webSocketService.NotifyUserAsync(nextPlayerId, "YourTurn", "Es tu turno de atacar.");
            }

            var actionDetails = $"Disparo en ({x}, {y}) {(ship != null ? (ship.IsSunk ? "¡Barco hundido!" : "¡Acierto!") : "Fallo.")}";
            return new ServiceResponse<string> { Success = true, Message = actionDetails };
        }
        finally
        {
            _turnLock.Release();
        }
    }







    private GameAction SimulateBotAttack(Board playerBoard)
    {

        var availableCells = playerBoard.Grid
            .Where(kvp => !kvp.Value.IsHit)
            .Select(kvp => kvp.Value)
            .ToList();

        if (!availableCells.Any())
        {
            throw new InvalidOperationException("No hay celdas disponibles para el ataque.");
        }

        var random = new Random();
        var targetCell = availableCells[random.Next(availableCells.Count)];

        targetCell.IsHit = true;

        var actionDetails = $"El bot dispara en ({targetCell.X}, {targetCell.Y})";

        var ship = playerBoard.Ships
            .FirstOrDefault(s => s.Coordinates.Any(coord => coord.X == targetCell.X && coord.Y == targetCell.Y));

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
        var participant = await _gameParticipantRepository.GetParticipantsByUserIdAsync(playerId);
        if (participant == null || !participant.Any())
            return new ServiceResponse<string> { Success = false, Message = "El jugador no está en ninguna partida activa." };

        foreach (var p in participant)
        {
            var game = await _gameRepository.GetByIdAsync(p.GameId);
            if (game == null || game.State == GameState.Finished) continue;

            await _gameParticipantRepository.RemoveAsync(p);

            var remainingParticipants = await _gameParticipantRepository.GetParticipantsByGameIdAsync(p.GameId);
            if (!remainingParticipants.Any())
            {
                game.State = GameState.WaitingForPlayers;
            }
            else
            {
                game.State = GameState.WaitingForPlayer1Ships;
                var remainingPlayer = remainingParticipants.First();
                game.CurrentPlayerId = remainingPlayer.UserId;
            }

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


        var participant = new GameParticipant
        {
            GameId = newGame.GameId,
            UserId = userIdInt,
            Role = ParticipantRole.Host,
            IsReady = false
        };

        await _gameParticipantRepository.AddAsync(participant);


        var botParticipant = new GameParticipant
        {
            GameId = newGame.GameId,
            UserId = -1,
            Role = ParticipantRole.Guest,
            IsReady = true
        };

        await _gameParticipantRepository.AddAsync(botParticipant);

        return new ServiceResponse<Game> { Success = true, Data = newGame };
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


}
