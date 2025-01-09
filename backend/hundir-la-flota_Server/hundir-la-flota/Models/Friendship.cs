using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Friendship
{
    public int Id { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }
    public User User { get; set; }

    [Required]
    [ForeignKey("Friend")]
    public int FriendId { get; set; }
    public User Friend { get; set; }

    [Required]
    public bool IsConfirmed { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ConfirmedAt { get; set; }
}
