using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using hundir_la_flota.Models;

public class Board
{
    public const int Size = 10;

    [JsonIgnore]
    private readonly Dictionary<(int, int), Cell> _grid;

    [JsonIgnore]
    public IReadOnlyDictionary<(int, int), Cell> Grid => _grid;

    [JsonPropertyName("Grid")]
    public Dictionary<string, Cell> GridForSerialization
    {
        get
        {
            return _grid.ToDictionary(
                kvp => $"{kvp.Key.Item1},{kvp.Key.Item2}",
                kvp => kvp.Value
            );
        }
        set
        {
            _grid.Clear();
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

    public Board()
    {
        _grid = new Dictionary<(int, int), Cell>();
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                _grid[(i, j)] = new Cell { X = i, Y = j };
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
            return false;

        foreach (var coord in ship.Coordinates)
        {
            var cell = _grid[(coord.X, coord.Y)];
            cell.HasShip = true;
        }

        Ships.Add(ship);
        return true;
    }
}
