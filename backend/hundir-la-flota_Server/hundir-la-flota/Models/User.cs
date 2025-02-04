using System.ComponentModel.DataAnnotations;

public class User
{
    public int Id { get; set; }

    [Required]
    public string Nickname { get; set; }

    [Required]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    public string Email { get; set; }

    [Required]
    public string PasswordHash { get; set; }

    public string AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLogin { get; set; }

    public string Role { get; set; } = "user";
    
    public bool IsBlocked { get; set; } = false; 
}
