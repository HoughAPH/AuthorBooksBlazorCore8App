using IdentityManagerServerApi.Data;
using Microsoft.EntityFrameworkCore;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Models;
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
                    //Id = b.Id,
                    Title = b.Title,
                    AuthorName = b.Author.Name,
                    AuthorId = b.AuthorId
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
                AuthorName = b.Author.Name,
                AuthorId = b.AuthorId
            }).ToListAsync();

            return books;
        }

        public async Task<BookDTO> GetBook(int id)
        {
            var book = await _dbContext.Books
         .Where(b => b.Id == id)
         .Include(b => b.Author)
         .Select(b => new BookDTO
         {
             Id = b.Id,
             Title = b.Title,
             AuthorName = b.Author.Name,
             AuthorId = b.AuthorId
         })
         .FirstOrDefaultAsync();

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

        public async Task<int> UpdateBook(int id, BookDTO book)
        {
            var bookToUpdate = await _dbContext.Books.FindAsync(id);

            if (bookToUpdate is null)
            {
                return 0;
            }

            bookToUpdate.Title = book.Title;
            bookToUpdate.AuthorId = book.AuthorId;

            return await _dbContext.SaveChangesAsync();
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
