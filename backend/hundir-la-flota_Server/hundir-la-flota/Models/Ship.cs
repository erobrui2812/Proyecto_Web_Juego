using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hundir_la_flota.Models
{
    public class Ship
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Size { get; set; }
        public List<Coordinate> Coordinates { get; set; } = new List<Coordinate>();
        public bool IsSunk => Coordinates.All(coord => coord.IsHit);
        public bool IsDamaged => Coordinates.Any(coord => coord.IsHit) && !IsSunk;

        public bool IsPlacementValid(int boardSize)
        {
            return Coordinates.All(coord => coord.IsValid(boardSize));
        }
    }

    public class Coordinate
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsHit { get; set; }

        public bool IsValid(int boardSize)
        {
            return X >= 0 && X < boardSize && Y >= 0 && Y < boardSize;
        }
    }
}
