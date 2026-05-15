using System.ComponentModel.DataAnnotations;

namespace SharedClassLibrary.DTOs;

public class AuthorDTO
{

    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = null!;
}
