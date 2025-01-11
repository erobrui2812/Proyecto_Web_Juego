namespace hundir_la_flota.Models
{
    public class Board
    {
        public const int Size = 10;
        public Cell[,] Grid { get; set; } = new Cell[Size, Size]; //Matriz
        public List<Ship> Ships { get; set; } = new List<Ship>();
    }
}