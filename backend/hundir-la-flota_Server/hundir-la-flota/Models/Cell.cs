namespace hundir_la_flota.Models
{
    public class Cell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool HasShip { get; set; }
        public bool IsHit { get; set; }
        public CellStatus Status { get; set; } = CellStatus.Empty;

        public bool IsValidCoordinate(int size)
        {
            return X >= 0 && X < size && Y >= 0 && Y < size;
        }
    }

    public enum CellStatus
    {
        Empty,
        Hit,
        Miss
    }
}
