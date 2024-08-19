using SharedClassLibrary.DTOs;

namespace SharedClassLibrary.Contracts
{
    public interface IBooks
    {
        Task<IEnumerable<BookDTO>> GetBooks();
        Task<IEnumerable<BookDTO>> GetAuthorBooks(int id);
        Task<BookDTO> GetBook(int id);
        Task<int> AddBook(BookDTO book);
        Task<int> UpdateBook(int id, BookDTO book);
        Task<bool> DeleteBook(int id);
    }
}
