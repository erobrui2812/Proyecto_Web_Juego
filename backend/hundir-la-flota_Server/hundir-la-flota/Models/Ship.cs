namespace hundir_la_flota.Models
{
    public class Ship
    {
        public string Name { get; set; }
        public int Size { get; set; }
        public List<Coordinate> Coordinates { get; set; }
        public bool IsSunk => Coordinates.All(coord => coord.IsHit); // Verificar si todas las celdas están marcadas como golpeadas
    }

    public class Coordinate
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsHit { get; set; }
    }
}
