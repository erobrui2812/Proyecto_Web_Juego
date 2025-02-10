namespace hundir_la_flota.DTOs
{
  public class ActiveGameDTO
    {
      public Guid GameId { get; set; }
      public string StateDescription { get; set; }
      public DateTime CreatedAt { get; set; }
      public List<ParticipantDTO> Participants { get; set; }


    }
}
