using System.ComponentModel.DataAnnotations;

namespace SharedClassLibrary.DTOs;

public class BookDTO
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    public string AuthorName { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "A valid Author must be selected.")]
    public int AuthorId { get; set; }
}

