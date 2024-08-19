using System.ComponentModel.DataAnnotations;

namespace SharedClassLibrary.DTOs
{
    public class Author
    {

        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        //public string? UserId { get; set; }
        public ICollection<Book>? Books { get; set; }
    }
}
