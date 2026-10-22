using System.ComponentModel.DataAnnotations;

namespace SharedClassLibrary.DTOs;

public class AuthorDTO
{

    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>Concurrency token from the last read; send it back on update to detect a stale edit. Optional.</summary>
    public Guid? Version { get; set; }
}
