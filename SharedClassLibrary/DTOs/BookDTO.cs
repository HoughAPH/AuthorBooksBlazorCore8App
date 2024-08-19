using System.ComponentModel.DataAnnotations;

namespace SharedClassLibrary.DTOs
{

    public class BookDTO
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        public string? AuthorName { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "AuthorId must be greater than 0.")]
        public int AuthorId { get; set; }
    }
}

