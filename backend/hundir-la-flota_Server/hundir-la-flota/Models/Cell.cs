using System.ComponentModel.DataAnnotations;

namespace hundir_la_flota.Models
{
    public class Cell
    {
        [Range(0, 9, ErrorMessage = "La coordenada X debe estar entre 0 y 9.")]
        public int X { get; set; }

        [Range(0, 9, ErrorMessage = "La coordenada Y debe estar entre 0 y 9.")]
        public int Y { get; set; }

        public bool HasShip { get; set; }
        public bool IsHit { get; set; }
        public CellStatus Status { get; set; } = CellStatus.Empty;
    }

    public enum CellStatus
    {
        Empty,
        Hit,
        Miss
    }
}
