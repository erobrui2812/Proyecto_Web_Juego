namespace hundir_la_flota.DTOs
{
    public class GameResponseDTO
    {
        public Guid GameId { get; set; }
        public string Player1Nickname { get; set; }
        public string Player2Nickname { get; set; }
        public string Player1Display { get; set; }
        public string Player2Display { get; set; }
        public string Player1Role { get; set; }
        public string Player2Role { get; set; }
        public string StateDescription { get; set; }
        public Board Player1Board { get; set; }
        public Board Player2Board { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
