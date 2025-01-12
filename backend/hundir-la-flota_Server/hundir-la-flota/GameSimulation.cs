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

    private List<Coordinate> GetAdjacentCoordinates(Coordinate hitCoordinate)
    {
        var adjacentCoordinates = new List<Coordinate>
    {
        new Coordinate { X = hitCoordinate.X - 1, Y = hitCoordinate.Y }, // Izquierda
        new Coordinate { X = hitCoordinate.X + 1, Y = hitCoordinate.Y }, // Derecha
        new Coordinate { X = hitCoordinate.X, Y = hitCoordinate.Y - 1 }, // Arriba
        new Coordinate { X = hitCoordinate.X, Y = hitCoordinate.Y + 1 }  // Abajo
    };

        // Filtrar coordenadas dentro del tablero
        return adjacentCoordinates.Where(c => c.X >= 0 && c.X < Board.Size && c.Y >= 0 && c.Y < Board.Size).ToList();
    }


    private readonly IGameService _gameService;

    public GameSimulation(IGameService gameService)
    {
        _gameService = gameService;
    }

    private Coordinate GenerateRandomCoordinate(List<Coordinate> attackHistory)
    {
        Random rand = new Random();
        Coordinate randomCoord;
        do
        {
            randomCoord = new Coordinate
            {
                X = rand.Next(0, Board.Size),
                Y = rand.Next(0, Board.Size)
            };
        } while (attackHistory.Contains(randomCoord));
        return randomCoord;
    }

    private Ship GetShipAtCoordinate(Board board, Coordinate coordinate)
    {
        foreach (var ship in board.Ships)
        {
            if (ship.Coordinates.Any(c => c.X == coordinate.X && c.Y == coordinate.Y))
            {
                return ship;
            }
        }
        return null;
    }


    public async Task RunSimulationAsync()
    {



        var gameRepository = new GameRepository();
        var gameService = new GameService(gameRepository);

        // Paso 1: Crear una partida
        Console.WriteLine("Creando partida...");
        var createGameResponse = await gameService.CreateGameAsync("1");
        if (!createGameResponse.Success)
        {
            Console.WriteLine($"Error al crear la partida: {createGameResponse.Message}");
            return;
        }
        var game = createGameResponse.Data;
        Console.WriteLine($"Partida creada. ID: {game.GameId}");

        // Paso 2: Unirse como segundo jugador
        Console.WriteLine("Unirse como segundo jugador...");
        var joinGameResponse = await gameService.JoinGameAsync(game.GameId, 2);
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
        var player2Ships = GenerateShipsForPlayer(game.Player2Board);
        var placeShipsResponse2 = await gameService.PlaceShipsAsync(game.GameId, 2, player2Ships);
        if (!placeShipsResponse2.Success)
        {
            Console.WriteLine($"Error al colocar barcos: {placeShipsResponse2.Message}");
            return;
        }
        Console.WriteLine("Barcos del jugador 2 colocados.");
        // Paso 5: Simulacion de ataques hasta perder
        Random rand = new Random();
        bool gameOver = false;
        List<Coordinate> attackHistory1 = new List<Coordinate>();
        List<Coordinate> attackHistory2 = new List<Coordinate>();
        List<Coordinate> searchAround1 = new List<Coordinate>();
        List<Coordinate> searchAround2 = new List<Coordinate>();

        while (!gameOver)
        {
            // Jugador 1 ataca
            Coordinate attackCoord1;
            if (searchAround1.Any())
            {
                attackCoord1 = searchAround1.First();
                searchAround1.RemoveAt(0);
            }
            else
            {
                attackCoord1 = GenerateRandomCoordinate(attackHistory1);
            }

            var attackResponse1 = await gameService.AttackAsync(game.GameId, 1, attackCoord1.X, attackCoord1.Y);
            Console.WriteLine($"Jugador 1 ataca ({attackCoord1.X}, {attackCoord1.Y}): {attackResponse1.Message}");

            attackHistory1.Add(attackCoord1);

            if (attackResponse1.Message.Contains("Impacto"))
            {
                var ship = GetShipAtCoordinate(game.Player1Board, attackCoord1);
                if (ship != null && ship.Size > 1)
                {
                    // Si el barco tiene más de 1 casilla, buscar alrededor
                    var adjacentCoords = GetAdjacentCoordinates(attackCoord1);
                    searchAround1.AddRange(adjacentCoords.Where(c => !attackHistory1.Contains(c) && !searchAround1.Contains(c)));
                }
            }

            // Comprobar si el jugador 1 ha perdido
            if (HasPlayerLost(game.Player1Board))
            {
                Console.WriteLine("Jugador 1 ha perdido.");
                gameOver = true;
                break;
            }

            // Jugador 2 ataca
            Coordinate attackCoord2;
            if (searchAround2.Any())
            {
                attackCoord2 = searchAround2.First();
                searchAround2.RemoveAt(0);
            }
            else
            {
                attackCoord2 = GenerateRandomCoordinate(attackHistory2);
            }

            var attackResponse2 = await gameService.AttackAsync(game.GameId, 2, attackCoord2.X, attackCoord2.Y);
            Console.WriteLine($"Jugador 2 ataca ({attackCoord2.X}, {attackCoord2.Y}): {attackResponse2.Message}");

            attackHistory2.Add(attackCoord2);

            if (attackResponse2.Message.Contains("Impacto"))
            {
                var ship = GetShipAtCoordinate(game.Player2Board, attackCoord2);
                if (ship != null && ship.Size > 1)
                {

                    var adjacentCoords = GetAdjacentCoordinates(attackCoord2);
                    searchAround2.AddRange(adjacentCoords.Where(c => !attackHistory2.Contains(c) && !searchAround2.Contains(c)));
                }
            }


            if (HasPlayerLost(game.Player2Board))
            {
                Console.WriteLine("Jugador 2 ha perdido.");
                gameOver = true;
                break;
            }
        }

        // Mostrar los tableros de ambos jugadores
        Console.WriteLine("Tablero del Jugador 1:");
        PrintBoard(game.Player1Board);
        Console.WriteLine("Tablero del Jugador 2:");
        PrintBoard(game.Player2Board);

        Console.WriteLine("Simulación completada.");
    }


    private void PrintBoard(Board board)
    {
        for (int y = 0; y < Board.Size; y++)
        {
            for (int x = 0; x < Board.Size; x++)
            {
                var cell = board.Grid[x, y];

                if (cell.IsHit)
                {

                    Console.Write("º ");
                }
                else if (board.Ships.Exists(s => s.Coordinates.Exists(c => c.X == x && c.Y == y)))
                {

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


    private List<Ship> GenerateShipsForPlayer(Board board)
    {
        var ships = new List<Ship>();
        Random rand = new Random();


        ships.Add(new Ship
        {
            Name = "Barco 4",
            Size = 4,
            Coordinates = GenerateShipCoordinates(4, board)
        });

        ships.Add(new Ship
        {
            Name = "Barco 3A",
            Size = 3,
            Coordinates = GenerateShipCoordinates(3, board)
        });

        ships.Add(new Ship
        {
            Name = "Barco 3B",
            Size = 3,
            Coordinates = GenerateShipCoordinates(3, board)
        });

        ships.Add(new Ship
        {
            Name = "Barco 2A",
            Size = 2,
            Coordinates = GenerateShipCoordinates(2, board)
        });

        ships.Add(new Ship
        {
            Name = "Barco 2B",
            Size = 2,
            Coordinates = GenerateShipCoordinates(2, board)
        });

        ships.Add(new Ship
        {
            Name = "Barco 2C",
            Size = 2,
            Coordinates = GenerateShipCoordinates(2, board)
        });

        ships.Add(new Ship
        {
            Name = "Barco 1A",
            Size = 1,
            Coordinates = GenerateShipCoordinates(1, board)
        });

        ships.Add(new Ship
        {
            Name = "Barco 1B",
            Size = 1,
            Coordinates = GenerateShipCoordinates(1, board)
        });

        ships.Add(new Ship
        {
            Name = "Barco 1C",
            Size = 1,
            Coordinates = GenerateShipCoordinates(1, board)
        });

        ships.Add(new Ship
        {
            Name = "Barco 1D",
            Size = 1,
            Coordinates = GenerateShipCoordinates(1, board)
        });

        return ships;
    }


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


            if (isVertical)
            {

                if (y + shipSize > Board.Size)
                    continue;

                for (int i = 0; i < shipSize; i++)
                {
                    var coord = new Coordinate { X = x, Y = y + i };
                    coordinates.Add(coord);
                }
            }
            else
            {

                if (x + shipSize > Board.Size)
                    continue;

                for (int i = 0; i < shipSize; i++)
                {
                    var coord = new Coordinate { X = x + i, Y = y };
                    coordinates.Add(coord);
                }
            }

        } while (!IsShipPlacementValid(board, coordinates));


        foreach (var coord in coordinates)
        {
            occupiedCoordinates.Add(coord);
        }

        // Log de las coordenadas generadas correctamente
        Console.WriteLine($"Barco de tamaño {shipSize} colocado en: {string.Join(", ", coordinates.Select(c => $"({c.X},{c.Y})"))}");

        return coordinates;
    }

    private bool HasPlayerLost(Board board)
    {

        foreach (var ship in board.Ships)
        {
            if (ship.Coordinates.Any(c => !board.Grid[c.X, c.Y].IsHit))
            {
                return false;
            }
        }
        return true; // Todos los barcos estan hundidos
    }


    private bool IsShipPlacementValid(Board board, List<Coordinate> coordinates)
    {
        foreach (var coordinate in coordinates)
        {

            if (coordinate.X < 0 || coordinate.X >= Board.Size || coordinate.Y < 0 || coordinate.Y >= Board.Size)
            {
                return false;
            }


            if (occupiedCoordinates.Contains(coordinate))
            {
                Console.WriteLine($"Celda ocupada en ({coordinate.X}, {coordinate.Y})");
                return false;
            }

            var cell = board.Grid[coordinate.X, coordinate.Y];
            if (cell.Status != CellStatus.Empty)
            {
                Console.WriteLine($"Celda ocupada en ({coordinate.X}, {coordinate.Y}) con estado {cell.Status}");
                return false;
            }
        }
        return true;
    }
}
