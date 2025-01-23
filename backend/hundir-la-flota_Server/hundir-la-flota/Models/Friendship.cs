using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Friendship
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User User { get; set; }

    [Required]
    public int FriendId { get; set; }
    public User Friend { get; set; }

    public bool IsConfirmed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }

    public FriendshipStatus Status => IsConfirmed ? FriendshipStatus.Accepted : FriendshipStatus.Pending;

    public enum FriendshipStatus
    {
        Pending,
        Accepted
    }
}
