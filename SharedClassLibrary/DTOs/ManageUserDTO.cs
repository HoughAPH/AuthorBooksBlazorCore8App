using System.ComponentModel.DataAnnotations;
namespace SharedClassLibrary.DTOs;
public class ManageUserDTO
{
    public string Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }
    public string Role { get; set; }
}
