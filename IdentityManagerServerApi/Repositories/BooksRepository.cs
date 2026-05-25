using IdentityManagerServerApi.Data;
using Microsoft.EntityFrameworkCore;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Models;
using GeneralResponse = SharedClassLibrary.Models.ServiceResponses.GeneralResponse;

namespace IdentityManagerServerApi.Repositories;

public class BooksRepository : IBooks
{
    private readonly AppDbContext _dbContext;

    public BooksRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<BookDTO>> GetBooks()
    {
        List<BookDTO> books = await _dbContext.Books
            .Include(b => b.Author)
            .Select(b => new BookDTO
            {
                Id = b.Id,
                Title = b.Title,
                AuthorName = b.Author.Name,
                AuthorId = b.AuthorId
            })
            .ToListAsync();

        return books;
    }

    public async Task<IEnumerable<BookDTO>> GetAuthorBooks(int id)
    {
        List<BookDTO> books = await _dbContext.Books
            .Where(b => b.AuthorId == id)
            .Include(b => b.Author)
            .Select(b => new BookDTO
            {
                Id = b.Id,
                Title = b.Title,
                AuthorName = b.Author.Name,
                AuthorId = b.AuthorId
            })
            .ToListAsync();

        return books;
    }

    public async Task<BookDTO?> GetBook(int id)
    {
        BookDTO? book = await _dbContext.Books
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

    public async Task<GeneralResponse> AddBook(BookDTO book)
    {
        _dbContext.Books.Add(new Book
        {
            Title = book.Title,
            AuthorId = book.AuthorId
        });

        int rowsAffected = await _dbContext.SaveChangesAsync();

        return rowsAffected > 0
            ? new(true, "Book added successfully.")
            : new(false, "Book could not be added.");
    }

    public async Task<GeneralResponse> UpdateBook(int id, BookDTO book)
    {
        Book? bookToUpdate = await _dbContext.Books.FindAsync(id);

        if (bookToUpdate is null)
            return new(false, "Book not found.");

        bookToUpdate.Title = book.Title;
        bookToUpdate.AuthorId = book.AuthorId;

        int rowsAffected = await _dbContext.SaveChangesAsync();

        return rowsAffected > 0
            ? new(true, "Book updated successfully.")
            : new(false, "Book could not be updated.");
    }

    public async Task<GeneralResponse> DeleteBook(int id)
    {
        Book? book = await _dbContext.Books.FindAsync(id);

        if (book is null)
            return new(false, "Book not found.");

        _dbContext.Books.Remove(book);

        int rowsAffected = await _dbContext.SaveChangesAsync();

        if (rowsAffected <= 0)
            throw new InvalidOperationException("Book could not be deleted.");

        return new(true, "Book deleted successfully.");
    }
}
