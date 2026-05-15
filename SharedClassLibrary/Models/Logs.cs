using System.ComponentModel.DataAnnotations;

namespace SharedClassLibrary.Models;

public class Logs
{
    [Key]
    public int Id { get; set; }
    public string? Message { get; set; } = null!;
    public string? MessageTemplate { get; set; } = null!;
    public string? Level { get; set; } = null!;
    public DateTimeOffset TimeStamp { get; set; }
    public string? Exception { get; set; } = null!;
    public string? Properties { get; set; } = null!;
    public string? LogEvent { get; set; } = null!;
    public string? OtherData { get; set; } = null!;
}
