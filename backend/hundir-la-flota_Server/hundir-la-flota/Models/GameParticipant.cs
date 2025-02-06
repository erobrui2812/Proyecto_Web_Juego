using hundir_la_flota.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class GameParticipant
{
    [Key]
    public int Id { get; set; }

    public Guid GameId { get; set; }
    public Game Game { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public ParticipantRole Role { get; set; }
    public bool IsReady { get; set; } = false;

    public bool Abandoned { get; set; } = false;
}

public enum ParticipantRole
{
    Host,
    Guest
}
