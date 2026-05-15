using System.ComponentModel.DataAnnotations;

namespace SharedClassLibrary.Models;

public class Book
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Title { get; set; } = null!;
    [Required]
    public int AuthorId { get; set; }
    public virtual Author? Author { get; set; }
}
