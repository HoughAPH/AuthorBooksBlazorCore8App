using SharedClassLibrary.DTOs;
using SharedClassLibrary.Models;

namespace SharedClassLibrary.Contracts;

public interface IBooks
{
    Task<IEnumerable<BookDTO>> GetBooks();
    Task<IEnumerable<BookDTO>> GetAuthorBooks(int id);
    Task<BookDTO?> GetBook(int id);
    Task<PagedResult<BookDTO>> GetBooksPage(int pageNumber, int pageSize, string? search = null, int? authorId = null);
    Task<Result> AddBook(BookDTO book);
    Task<Result> UpdateBook(int id, BookDTO book);
    Task<Result> DeleteBook(int id);
}
