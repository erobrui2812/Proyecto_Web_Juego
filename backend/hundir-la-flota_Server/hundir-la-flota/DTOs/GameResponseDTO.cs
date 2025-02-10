using hundir_la_flota.Models;

namespace hundir_la_flota.DTOs
{
    public class GameResponseDTO
    {
        public Guid GameId { get; set; }
        public string Player1Nickname { get; set; }
        public string Player2Nickname { get; set; }
        public string Player1Role { get; set; }
        public string Player2Role { get; set; }
        public string Player1Display => $"{Player1Nickname} - {Player1Role}";
        public string Player2Display => $"{Player2Nickname} - {Player2Role}";
        public string StateDescription { get; set; }
        public BoardDTO Player1Board { get; set; }
        public BoardDTO Player2Board { get; set; }
        public List<GameActionDTO> Actions { get; set; }
        public int CurrentPlayerId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
