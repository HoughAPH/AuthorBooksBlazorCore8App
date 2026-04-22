using System.ComponentModel.DataAnnotations;

namespace SharedClassLibrary.Models;

public class Book
{
    public int Id { get; set; }
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    public int AuthorId { get; set; }
    public virtual Author? Author { get; set; }
}
