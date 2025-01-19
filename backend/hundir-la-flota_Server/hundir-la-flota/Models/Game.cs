using System.ComponentModel;

namespace hundir_la_flota.Models
{
    public class Game
    {
        public Guid GameId { get; set; } = Guid.NewGuid();
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public GameState State { get; set; } = GameState.WaitingForPlayers;
        public Board Player1Board { get; set; } = new Board();
        public Board Player2Board { get; set; } = new Board();
        public int CurrentPlayerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? WinnerId { get; set; }

        public List<GameAction> Actions { get; set; } = new List<GameAction>();

    }

    public class GameAction
    {
        public int PlayerId { get; set; }
        public string ActionType { get; set; } // E.g., "Shot", "PlacedShip"
        public DateTime Timestamp { get; set; }
        public string Details { get; set; } // Coordenadas o resultados
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
