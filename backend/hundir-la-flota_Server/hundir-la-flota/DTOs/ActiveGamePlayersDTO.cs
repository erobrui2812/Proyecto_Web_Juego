namespace hundir_la_flota.DTOs
{
    public class ActiveGamePlayersDTO
    {
        public Guid GameId { get; set; }
        public ParticipantDTO Player1 { get; set; }
        public ParticipantDTO Player2 { get; set; }
    }
}
