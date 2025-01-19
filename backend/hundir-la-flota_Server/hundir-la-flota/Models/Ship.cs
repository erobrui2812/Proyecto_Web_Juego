using System.ComponentModel.DataAnnotations;

namespace hundir_la_flota.Models
{
    public class Ship
    {
        public string Name { get; set; }
        public int Size { get; set; }
        public List<Coordinate> Coordinates { get; set; }
        public bool IsSunk => Coordinates.All(coord => coord.IsHit);
        public bool IsDamaged => Coordinates.Any(c => c.IsHit) && !IsSunk;

    }

    public class Coordinate
    {
        [Range(0, 9, ErrorMessage = "La coordenada X debe estar entre 0 y 9.")]
        public int X { get; set; }

        [Range(0, 9, ErrorMessage = "La coordenada Y debe estar entre 0 y 9.")]
        public int Y { get; set; }

        public bool IsHit { get; set; }
    }

}
