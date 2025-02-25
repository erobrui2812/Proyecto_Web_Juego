public static class InvitationStorage
{
    public static Dictionary<string, Invitation> PendingInvitations { get; set; }
        = new Dictionary<string, Invitation>();
}

public class Invitation
{
    public int HostId { get; set; }
    public int GuestId { get; set; }
    public DateTime CreatedAt { get; set; }
}
