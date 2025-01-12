using hundir_la_flota.Models;
using hundir_la_flota.Repositories;


public class CoordinateComparer : IEqualityComparer<Coordinate>
{
    public bool Equals(Coordinate x, Coordinate y)
    {
        return x.X == y.X && x.Y == y.Y;
    }

    public int GetHashCode(Coordinate obj)
    {
        return obj.X.GetHashCode() ^ obj.Y.GetHashCode();
    }
}


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
        var player1Ships = GenerateShipsForPlayer(game.Player1Board); // Pasa el tablero del jugador 1
        var placeShipsResponse1 = await gameService.PlaceShipsAsync(game.GameId, 1, player1Ships);
        if (!placeShipsResponse1.Success)
        {
            Console.WriteLine($"Error al colocar barcos: {placeShipsResponse1.Message}");
            return;
        }
        Console.WriteLine("Barcos del jugador 1 colocados.");

        // Paso 4: Colocar barcos para el jugador 2
        Console.WriteLine("Colocando barcos para el jugador 2...");
        var player2Ships = GenerateShipsForPlayer(game.Player2Board); // Pasa el tablero del jugador 2
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

    // Generación de los barcos para un jugador
    private List<Ship> GenerateShipsForPlayer(Board board)
    {
        var ships = new List<Ship>();
        Random rand = new Random();

        // Crear barcos de diferentes tamaños (como se especifica en el enunciado)
        ships.Add(new Ship
        {
            Name = "Barco 4",
            Size = 4,
            Coordinates = GenerateShipCoordinates(4, board) // Ahora le pasamos el tablero
        });

        ships.Add(new Ship
        {
            Name = "Barco 3A",
            Size = 3,
            Coordinates = GenerateShipCoordinates(3, board) // Ahora le pasamos el tablero
        });

        ships.Add(new Ship
        {
            Name = "Barco 3B",
            Size = 3,
            Coordinates = GenerateShipCoordinates(3, board) // Ahora le pasamos el tablero
        });

        ships.Add(new Ship
        {
            Name = "Barco 2A",
            Size = 2,
            Coordinates = GenerateShipCoordinates(2, board) // Ahora le pasamos el tablero
        });

        ships.Add(new Ship
        {
            Name = "Barco 2B",
            Size = 2,
            Coordinates = GenerateShipCoordinates(2, board) // Ahora le pasamos el tablero
        });

        ships.Add(new Ship
        {
            Name = "Barco 2C",
            Size = 2,
            Coordinates = GenerateShipCoordinates(2, board) // Ahora le pasamos el tablero
        });

        ships.Add(new Ship
        {
            Name = "Barco 1A",
            Size = 1,
            Coordinates = GenerateShipCoordinates(1, board) // Ahora le pasamos el tablero
        });

        ships.Add(new Ship
        {
            Name = "Barco 1B",
            Size = 1,
            Coordinates = GenerateShipCoordinates(1, board) // Ahora le pasamos el tablero
        });

        ships.Add(new Ship
        {
            Name = "Barco 1C",
            Size = 1,
            Coordinates = GenerateShipCoordinates(1, board) // Ahora le pasamos el tablero
        });

        ships.Add(new Ship
        {
            Name = "Barco 1D",
            Size = 1,
            Coordinates = GenerateShipCoordinates(1, board) // Ahora le pasamos el tablero
        });

        return ships;
    }

    // Generación de las coordenadas para cada barco
    private HashSet<Coordinate> occupiedCoordinates = new HashSet<Coordinate>(new CoordinateComparer());

    private List<Coordinate> GenerateShipCoordinates(int shipSize, Board board)
    {
        Random rand = new Random();
        int x, y;
        bool isVertical;
        List<Coordinate> coordinates;

        do
        {
            x = rand.Next(0, Board.Size);
            y = rand.Next(0, Board.Size);
            isVertical = rand.Next(0, 2) == 0; // Determina si el barco será vertical o horizontal

            coordinates = new List<Coordinate>();

            // Verificar si la colocación es válida según la orientación
            if (isVertical)
            {
                // Verificar que haya suficiente espacio en la vertical
                if (y + shipSize > Board.Size)
                    continue; // Si no hay espacio, vuelve a intentar

                for (int i = 0; i < shipSize; i++)
                {
                    var coord = new Coordinate { X = x, Y = y + i };
                    coordinates.Add(coord);
                }
            }
            else
            {
                // Verificar que haya suficiente espacio en la horizontal
                if (x + shipSize > Board.Size)
                    continue; // Si no hay espacio, vuelve a intentar

                for (int i = 0; i < shipSize; i++)
                {
                    var coord = new Coordinate { X = x + i, Y = y };
                    coordinates.Add(coord);
                }
            }

        } while (!IsShipPlacementValid(board, coordinates)); // Reintentar si la colocación no es válida

        // Registrar las coordenadas ocupadas
        foreach (var coord in coordinates)
        {
            occupiedCoordinates.Add(coord);
        }

        // Log de las coordenadas generadas correctamente
        Console.WriteLine($"Barco de tamaño {shipSize} colocado en: {string.Join(", ", coordinates.Select(c => $"({c.X},{c.Y})"))}");

        return coordinates;
    }

    private bool IsShipPlacementValid(Board board, List<Coordinate> coordinates)
    {
        foreach (var coordinate in coordinates)
        {
            // Verificar si las coordenadas están dentro del tablero
            if (coordinate.X < 0 || coordinate.X >= Board.Size || coordinate.Y < 0 || coordinate.Y >= Board.Size)
            {
                return false; // Coordenada fuera de los límites del tablero
            }

            // Verificar si la posición ya está ocupada por otro barco
            if (occupiedCoordinates.Contains(coordinate))
            {
                Console.WriteLine($"Celda ocupada en ({coordinate.X}, {coordinate.Y})");
                return false; // La celda ya está ocupada
            }

            var cell = board.Grid[coordinate.X, coordinate.Y];
            if (cell.Status != CellStatus.Empty)
            {
                Console.WriteLine($"Celda ocupada en ({coordinate.X}, {coordinate.Y}) con estado {cell.Status}");
                return false; // La celda ya está ocupada
            }
        }
        return true; // La colocación es válida
    }


}
