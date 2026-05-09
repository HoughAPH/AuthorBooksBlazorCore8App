namespace SharedClassLibrary.DTOs;

public class CurrentUserDto
{
    public bool IsAuthenticated { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}