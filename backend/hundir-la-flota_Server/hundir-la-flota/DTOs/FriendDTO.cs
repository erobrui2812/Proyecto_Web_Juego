using System.ComponentModel.DataAnnotations;

public class FriendDto
{
    [Required]
    public int FriendId { get; set; }

    [Required]
    [MaxLength(50, ErrorMessage = "El nickname no debe exceder los 50 caracteres.")]
    public string FriendNickname { get; set; }

    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    public string FriendMail { get; set; }

    [Url(ErrorMessage = "La URL del avatar no es válida.")]
    public string AvatarUrl { get; set; }

    public string Status { get; set; } = "Disconnected";
}
