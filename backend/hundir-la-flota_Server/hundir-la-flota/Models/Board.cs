using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using hundir_la_flota.Models;

public class Board
{
    public const int Size = 10;

    // Se elimina el modificador readonly para poder reinicializarlo
    private Dictionary<(int, int), Cell> _grid;

    // Al acceder a Grid se inicializa _grid si es null
    [NotMapped]
    [JsonIgnore]
    public IReadOnlyDictionary<(int, int), Cell> Grid
    {
        get
        {
            if (_grid == null)
            {
                InitializeGrid();
                
                foreach (var ship in Ships)
                {
                    foreach (var coord in ship.Coordinates)
                    {
                        
                        if (_grid.ContainsKey((coord.X, coord.Y)))
                        {
                            _grid[(coord.X, coord.Y)].HasShip = true;
                        }
                    }
                }
            }
            return _grid;
        }
    }


    // Propiedad para la serialización (EF Core la ignora)
    [JsonPropertyName("Grid")]
    public Dictionary<string, Cell> GridForSerialization
    {
        get
        {
            // Aseguramos que el grid esté inicializado
            return Grid.ToDictionary(
                kvp => $"{kvp.Key.Item1},{kvp.Key.Item2}",
                kvp => kvp.Value
            );
        }
        set
        {
            _grid = new Dictionary<(int, int), Cell>();
            foreach (var kvp in value)
            {
                var parts = kvp.Key.Split(',');
                var x = int.Parse(parts[0]);
                var y = int.Parse(parts[1]);
                _grid[(x, y)] = kvp.Value;
            }
        }
    }

    public List<Ship> Ships { get; private set; } = new List<Ship>();

    // El constructor se utiliza al crear la entidad manualmente, pero no siempre se invoca al materializarla desde la BD
    public Board()
    {
        InitializeGrid();
    }

    // Inicializa el diccionario _grid con todas las celdas necesarias
    private void InitializeGrid()
    {
        _grid = new Dictionary<(int, int), Cell>();
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                _grid[(i, j)] = new Cell
                {
                    X = i,
                    Y = j,
                    HasShip = false,
                    IsHit = false,
                    Status = CellStatus.Empty
                };
            }
        }
    }

    public bool ProcessShot(int x, int y)
    {
        if (!IsWithinBounds(x, y))
            return false;

        var cell = _grid[(x, y)];
        if (cell.HasShip)
        {
            cell.IsHit = true;
            cell.Status = CellStatus.Hit;
            return true;
        }

        cell.Status = CellStatus.Miss;
        return false;
    }

    public bool IsShipPlacementValid(Ship ship)
    {
        foreach (var coord in ship.Coordinates)
        {
            if (!IsWithinBounds(coord.X, coord.Y) || _grid[(coord.X, coord.Y)].HasShip)
            {
                return false;
            }
        }
        return true;
    }

    public bool AreAllShipsSunk()
    {
        return Ships.Any() && Ships.All(ship => ship.IsSunk);
    }

    private bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < Size && y >= 0 && y < Size;
    }

    public bool PlaceShip(Ship ship)
    {
        if (!IsShipPlacementValid(ship))
        {
            System.Console.WriteLine($"[PlaceShip] Posición inválida para el barco {ship.Name} en las coordenadas: {string.Join(", ", ship.Coordinates.Select(c => $"({c.X},{c.Y})"))}");
            return false;
        }

        foreach (var coord in ship.Coordinates)
        {
            var cell = _grid[(coord.X, coord.Y)];
            cell.HasShip = true;
        }

        System.Console.WriteLine($"[PlaceShip] Colocando barco {ship.Name} con coordenadas: {string.Join(", ", ship.Coordinates.Select(c => $"({c.X},{c.Y})"))}");
        Ships.Add(ship);
        return true;
    }
}
