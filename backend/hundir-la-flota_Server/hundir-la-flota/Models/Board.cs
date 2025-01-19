using hundir_la_flota.Models;
using System.Collections.Generic;
using System.Linq;

public class Board
{
    public const int Size = 10;


    public List<Cell> Grid { get; set; } = new List<Cell>();
    public List<Ship> Ships { get; set; } = new List<Ship>();

    public Board()
    {

        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                Grid.Add(new Cell { X = i, Y = j });
            }
        }
    }

    public bool ProcessShot(int x, int y)
    {
        var cell = Grid.FirstOrDefault(c => c.X == x && c.Y == y);
        if (cell == null) return false;

        if (cell.HasShip)
        {
            cell.IsHit = true;
            cell.Status = CellStatus.Hit;
            return true; // Acierto
        }
        cell.Status = CellStatus.Miss;
        return false; // Fallo
    }

    public bool IsShipPlacementValid(Ship ship)
    {
        foreach (var coord in ship.Coordinates)
        {
            if (coord.X < 0 || coord.X >= Size || coord.Y < 0 || coord.Y >= Size)
            {
                return false; // Coordenada fuera de los límites
            }

            var cell = Grid.FirstOrDefault(c => c.X == coord.X && c.Y == coord.Y);
            if (cell == null || cell.HasShip)
            {
                return false; // Ya hay un barco en esta posición
            }
        }

        return true;
    }
}
