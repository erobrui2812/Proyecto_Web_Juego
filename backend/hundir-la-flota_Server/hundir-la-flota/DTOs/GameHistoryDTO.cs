namespace hundir_la_flota.DTOs
{
    public class GameHistoryDTO
    {
        public Guid GameId { get; set; }
        public int Player1Id { get; set; }
        public string Player1Nickname { get; set; }
        public int Player2Id { get; set; }
        public string Player2Nickname { get; set; }
        public DateTime DatePlayed { get; set; }
        public string Result { get; set; }
    }
}