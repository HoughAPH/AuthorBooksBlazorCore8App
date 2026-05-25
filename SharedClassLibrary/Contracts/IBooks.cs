using SharedClassLibrary.DTOs;
using GeneralResponse = SharedClassLibrary.Models.ServiceResponses.GeneralResponse;

namespace SharedClassLibrary.Contracts;

public interface IBooks
{
    Task<IEnumerable<BookDTO>> GetBooks();
    Task<IEnumerable<BookDTO>> GetAuthorBooks(int id);
    Task<BookDTO?> GetBook(int id);
    Task<GeneralResponse> AddBook(BookDTO book);
    Task<GeneralResponse> UpdateBook(int id, BookDTO book);
    Task<GeneralResponse> DeleteBook(int id);
}
