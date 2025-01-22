namespace hundir_la_flota.Models
{
    public class Leaderboard
    {
        public int Rank { get; set; }
        public string PlayerNickname { get; set; }
        public int Wins { get; set; }
        public int TotalGames { get; set; }

        public double WinRate => TotalGames > 0 ? (double)Wins / TotalGames * 100 : 0;
    }
}
