public class UserUpdateDTO
{
    public int Id { get; set; }
    public string Nickname { get; set; }
    public string Email { get; set; }
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
}