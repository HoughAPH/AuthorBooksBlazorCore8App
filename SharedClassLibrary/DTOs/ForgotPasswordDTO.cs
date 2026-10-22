using System.ComponentModel.DataAnnotations;

namespace SharedClassLibrary.DTOs;

public class ForgotPasswordDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
