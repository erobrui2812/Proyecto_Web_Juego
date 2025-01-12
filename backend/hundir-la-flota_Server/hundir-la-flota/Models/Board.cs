using hundir_la_flota.Models;

public class Board
{
    public const int Size = 10;
    public Cell[,] Grid { get; set; }
    public List<Ship> Ships { get; set; } = new List<Ship>();

    public Board()
    {
        Grid = new Cell[Size, Size];
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                Grid[i, j] = new Cell { X = i, Y = j };
            }
        }
    }

    public bool IsShipPlacementValid(Ship ship)
    {
        // Verificar si las coordenadas están dentro de los límites del tablero
        foreach (var coord in ship.Coordinates)
        {
            if (coord.X < 0 || coord.X >= Size || coord.Y < 0 || coord.Y >= Size)
            {
                return false; // Coordenada fuera de los límites
            }
        }

        // Verificar si las coordenadas están libres (sin barcos)
        foreach (var coord in ship.Coordinates)
        {
            if (Grid[coord.X, coord.Y].HasShip)
            {
                return false; // Ya hay un barco en esta posición
            }
        }

        return true; // Las coordenadas son válidas para colocar el barco
    }
}