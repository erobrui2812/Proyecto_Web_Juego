using System.Text.Json.Serialization;


namespace hundir_la_flota.Models
{
    public class Game
    {
        public Guid GameId { get; set; } = Guid.NewGuid();
        public GameState State { get; set; } = GameState.WaitingForPlayers;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? WinnerId { get; set; }

        public Board Player1Board { get; set; } = new Board();
        public Board Player2Board { get; set; } = new Board();
        public int? CurrentPlayerId { get; set; }

        [JsonIgnore]
        public List<GameParticipant> Participants { get; set; } = new List<GameParticipant>();

        [JsonIgnore]
        public List<GameAction> Actions { get; set; } = new List<GameAction>();


    }

    public class GameAction
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public string ActionType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Details { get; set; }
    }

    public enum GameState
    {
        WaitingForPlayers,
        WaitingForPlayer1Ships,
        WaitingForPlayer2Ships,
        WaitingForPlayer1Shot,
        WaitingForPlayer2Shot,
        InProgress,
        Finished
    }
}
