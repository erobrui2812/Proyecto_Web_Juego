using hundir_la_flota.Models;
using System.Collections.Generic;
using System.Linq;

public class Board
{
    public const int Size = 10;


    private readonly Dictionary<(int X, int Y), Cell> _grid;
    public IReadOnlyDictionary<(int X, int Y), Cell> Grid => _grid;

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
        return Ships.All(ship => ship.IsSunk);
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
