using System.ComponentModel.DataAnnotations;

public class UserLoginDTO
{
    public string Nickname { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }
}
