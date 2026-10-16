using SharedClassLibrary.DTOs;
using SharedClassLibrary.Models;

namespace SharedClassLibrary.Contracts;

public interface IAuthors
{
    Task<List<AuthorDTO>> GetAuthors();
    Task<AuthorDTO?> GetAuthor(int id);
    Task<Result> AddAuthor(AuthorDTO author);
    Task<Result> UpdateAuthor(int id, AuthorDTO author);
    Task<Result> DeleteAuthor(int id);
}
