namespace hundir_la_flota.Models
{
    public class PlayerStats
    {
        public int UserId { get; set; }
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }


        public double WinRate => GamesPlayed > 0 ? (double)GamesWon / GamesPlayed * 100 : 0;
    }
}
