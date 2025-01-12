using System.ComponentModel.DataAnnotations;

public class UserLoginDTO
{
    public string NicknameMail { get; set; }

    [Required]
    public string Password { get; set; }
}
