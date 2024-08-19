using System.ComponentModel.DataAnnotations;

namespace SharedClassLibrary.DTOs
{
    public partial class Book
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public int AuthorId { get; set; }
        public virtual Author? Author { get; set; }
        //public string Author { get; set; } = string.Empty; 
        //public string Genre { get; set; } = string.Empty;
        ///public string? Description { get; set; } = string.Empty;
        //public string? CoverImage { get; set; } = string.Empty;

        //this should be the foreign key to the user when the author is a user as well
        // on second thought, don't link this to the identity user
        //        public string? UserId { get; set; } = string.Empty;
    }
}
