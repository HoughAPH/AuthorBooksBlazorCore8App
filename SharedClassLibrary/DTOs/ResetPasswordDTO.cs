using System.ComponentModel.DataAnnotations;

namespace SharedClassLibrary.DTOs;

public class ResetPasswordDTO
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    public string? Token { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
