using SharedClassLibrary.DTOs;

namespace SharedClassLibrary.Contracts;

public interface IAuthors
{
    Task<List<AuthorDTO>> GetAuthors();
    Task<AuthorDTO?> GetAuthor(int id);
    Task<int> AddAuthor(AuthorDTO author);
    Task<int> UpdateAuthor(int id, AuthorDTO author);
    Task<string> DeleteAuthor(int id);
}
