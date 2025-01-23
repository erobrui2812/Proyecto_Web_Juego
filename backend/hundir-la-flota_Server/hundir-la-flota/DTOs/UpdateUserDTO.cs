using System.ComponentModel.DataAnnotations;

public class UpdateUserDTO
{
    [MaxLength(50, ErrorMessage = "El nickname no debe exceder los 50 caracteres.")]
    public string Nickname { get; set; }

    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    public string Email { get; set; }

    [Url(ErrorMessage = "La URL del avatar no es válida.")]
    public string AvatarUrl { get; set; }
}
