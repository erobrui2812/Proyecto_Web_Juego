using hundir_la_flota.Models;
using hundir_la_flota.Repositories;

public class GameSimulation
{
    private readonly IGameService _gameService;

    public GameSimulation(IGameService gameService)
    {
        _gameService = gameService;
    }

    public async Task RunSimulationAsync()
    {
        // Repositorio y servicio de juegos
        var gameRepository = new GameRepository();
        var gameService = new GameService(gameRepository);

        // Paso 1: Crear una partida
        Console.WriteLine("Creando partida...");
        var createGameResponse = await gameService.CreateGameAsync("1"); // Player1Id = 1
        if (!createGameResponse.Success)
        {
            Console.WriteLine($"Error al crear la partida: {createGameResponse.Message}");
            return;
        }
        var game = createGameResponse.Data;
        Console.WriteLine($"Partida creada. ID: {game.GameId}");

        // Paso 2: Unirse como segundo jugador
        Console.WriteLine("Unirse como segundo jugador...");
        var joinGameResponse = await gameService.JoinGameAsync(game.GameId, 2); // Player2Id = 2
        if (!joinGameResponse.Success)
        {
            Console.WriteLine($"Error al unirse a la partida: {joinGameResponse.Message}");
            return;
        }
        Console.WriteLine("Jugador 2 se unió a la partida.");

        // Paso 3: Colocar barcos para el jugador 1
        Console.WriteLine("Colocando barcos para el jugador 1...");
        var player1Ships = new List<Ship>
        {
            new Ship
            {
                Name = "Barco 1",
                Size = 2,
                Coordinates = new List<Coordinate>
                {
                    new Coordinate { X = 0, Y = 0 },
                    new Coordinate { X = 0, Y = 1 }
                }
            }
        };
        var placeShipsResponse1 = await gameService.PlaceShipsAsync(game.GameId, 1, player1Ships);
        if (!placeShipsResponse1.Success)
        {
            Console.WriteLine($"Error al colocar barcos: {placeShipsResponse1.Message}");
            return;
        }
        Console.WriteLine("Barcos del jugador 1 colocados.");

        // Paso 4: Colocar barcos para el jugador 2
        Console.WriteLine("Colocando barcos para el jugador 2...");
        var player2Ships = new List<Ship>
        {
            new Ship
            {
                Name = "Barco 2",
                Size = 2,
                Coordinates = new List<Coordinate>
                {
                    new Coordinate { X = 1, Y = 0 },
                    new Coordinate { X = 1, Y = 1 }
                }
            }
        };
        var placeShipsResponse2 = await gameService.PlaceShipsAsync(game.GameId, 2, player2Ships);
        if (!placeShipsResponse2.Success)
        {
            Console.WriteLine($"Error al colocar barcos: {placeShipsResponse2.Message}");
            return;
        }
        Console.WriteLine("Barcos del jugador 2 colocados.");

        // Paso 5: Simular ataques
        Console.WriteLine("Simulando ataques...");
        var attackResponse1 = await gameService.AttackAsync(game.GameId, 1, 1, 0); // Jugador 1 ataca (1, 0)
        Console.WriteLine($"Jugador 1 ataca (1, 0): {attackResponse1.Message}");

        var attackResponse2 = await gameService.AttackAsync(game.GameId, 2, 0, 0); // Jugador 2 ataca (0, 0)
        Console.WriteLine($"Jugador 2 ataca (0, 0): {attackResponse2.Message}");

        // Paso 6: Ver estado final del juego
        var gameStateResponse = await gameService.GetGameStateAsync("1", game.GameId);
        if (gameStateResponse.Success)
        {
            var finalGameState = gameStateResponse.Data;
            Console.WriteLine($"Estado final del juego: {finalGameState.State}");
        }
        else
        {
            Console.WriteLine($"Error al obtener estado del juego: {gameStateResponse.Message}");
        }

        // Mostrar los tableros de ambos jugadores
        Console.WriteLine("Tablero del Jugador 1:");
        PrintBoard(game.Player1Board);
        Console.WriteLine("Tablero del Jugador 2:");
        PrintBoard(game.Player2Board);

        Console.WriteLine("Simulación completada.");
    }

    // Método para imprimir el tablero
    private void PrintBoard(Board board)
    {
        for (int y = 0; y < Board.Size; y++)
        {
            for (int x = 0; x < Board.Size; x++)
            {
                var cell = board.Grid[x, y];

                if (cell.IsHit)
                {
                    // Marca las celdas impactadas con "º"
                    Console.Write("º ");
                }
                else if (board.Ships.Exists(s => s.Coordinates.Exists(c => c.X == x && c.Y == y)))
                {
                    // Marca las celdas donde hay un barco con "X"
                    Console.Write("X ");
                }
                else
                {
                    Console.Write(". ");
                }
            }
            Console.WriteLine();
        }
    }
}
