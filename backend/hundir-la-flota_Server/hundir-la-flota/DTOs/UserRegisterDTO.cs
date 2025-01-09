using System.ComponentModel.DataAnnotations;

public class UserRegisterDTO
{
    [Required]
    public string Nickname { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [MinLength(6, ErrorMessage = "La contraseña tiene que tener al menos 6 carácteres")]
    public string Password { get; set; }

    [Required]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; }

    [Url(ErrorMessage = "Tu Avatar tiene que ser una URL válida")]
    public string AvatarUrl { get; set; }
}
