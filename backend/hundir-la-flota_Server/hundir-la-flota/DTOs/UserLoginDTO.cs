using System.ComponentModel.DataAnnotations;

public class UserLoginDTO
{
    [Required(ErrorMessage = "El nickname o correo es obligatorio.")]
    public string NicknameMail { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string Password { get; set; }
}
