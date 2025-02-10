namespace hundir_la_flota.DTOs
{
    public class ShipDTO
    {
        public string Name { get; set; }
        public int Size { get; set; }
        public List<CoordinateDTO> Coordinates { get; set; }
    }
}
