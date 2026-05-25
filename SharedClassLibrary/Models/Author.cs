using System.ComponentModel.DataAnnotations;

namespace SharedClassLibrary.Models;

public class Author
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public ICollection<Book> Books { get; set; } = [];
}
