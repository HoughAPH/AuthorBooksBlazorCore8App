using IdentityManagerServerApi.Data;
using Microsoft.EntityFrameworkCore;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
//using ILogger = Serilog.ILogger;

namespace SharedClassLibrary.Repositories
{
    public class BooksRepository : IBooks
    {
        private readonly AppDbContext _dbContext;
        //private readonly ILogger _logger;
        public BooksRepository(AppDbContext dbContext)
        {
            //_logger = logger;
            _dbContext = dbContext;
        }
        public async Task<IEnumerable<BookDTO>> GetBooks()
        {
            var books = await _dbContext.Books
                .Include(b => b.Author)
                .Select(b => new BookDTO
                {
                    Id = b.Id,
                    Title = b.Title,
                    AuthorName = b.Author.Name
                })
                .ToListAsync();



            return books;
        }

        public async Task<IEnumerable<BookDTO>> GetAuthorBooks(int id)
        {
            var books = await _dbContext.Books
            .Where(b => b.AuthorId == id)
            .Include(b => b.Author)
            .Select(b => new BookDTO
            {
                Id = b.Id,
                Title = b.Title,
                AuthorName = b.Author.Name
            }).ToListAsync();

            return books;
        }

        public async Task<BookDTO> GetBook(int id)
        {
            var book = await _dbContext.Books
                .Include(b => b.Author)
                .Select(b => new BookDTO
                {
                    Id = b.Id,
                    Title = b.Title,
                    AuthorName = b.Author.Name,
                    AuthorId = b.AuthorId
                })
                .FirstOrDefaultAsync(b => b.Id == id);

            return book;

        }

        public async Task<int> AddBook(BookDTO book)
        {
            _dbContext.Books.Add(new Book
            {
                Title = book.Title,
                AuthorId = book.AuthorId
            });


            return await _dbContext.SaveChangesAsync();

        }

        public Task<int> UpdateBook(int id, BookDTO book)
        {
            if (_dbContext.Books.Any(b => b.Id == id))
            {
                var bookToUpdate = new Book
                {
                    Id = book.Id,
                    Title = book.Title,
                    AuthorId = book.AuthorId
                };
                _dbContext.Books.Update(bookToUpdate);
                return _dbContext.SaveChangesAsync();
            }
            else
            {
                return Task.FromResult(0);
            }
        }

        public Task<bool> DeleteBook(int id)
        {
            if (_dbContext.Books.Any(b => b.Id == id))
            {
                _dbContext.Books.Remove(new Book { Id = id });
                return _dbContext.SaveChangesAsync().ContinueWith(task => task.Result > 0);
            }
            else
            {
                return Task.FromResult(false);
            }
        }
    }
}
