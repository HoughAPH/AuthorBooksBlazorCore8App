using SharedClassLibrary.DTOs;
using GeneralResponse = SharedClassLibrary.Models.ServiceResponses.GeneralResponse;

namespace SharedClassLibrary.Contracts;

public interface IAuthors
{
    Task<List<AuthorDTO>> GetAuthors();
    Task<AuthorDTO?> GetAuthor(int id);
    Task<GeneralResponse> AddAuthor(AuthorDTO author);
    Task<GeneralResponse> UpdateAuthor(int id, AuthorDTO author);
    Task<GeneralResponse> DeleteAuthor(int id);
}
