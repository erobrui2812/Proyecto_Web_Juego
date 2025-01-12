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
}
