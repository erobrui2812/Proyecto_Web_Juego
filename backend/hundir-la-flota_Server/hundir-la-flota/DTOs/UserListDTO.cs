public class UserListDTO
{
    public int Id { get; set; }
    public string Nickname { get; set; }
    public string Email { get; set; }
    public string AvatarUrl { get; set; }
    public string Role { get; set; } = "user";
    public bool IsBlocked { get; set; } = false;
}
