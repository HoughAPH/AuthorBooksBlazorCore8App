using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Entities;
using IdentityManagerServerApi.Exceptions;
using Microsoft.EntityFrameworkCore;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using GeneralResponse = SharedClassLibrary.Models.ServiceResponses.GeneralResponse;

namespace IdentityManagerServerApi.Repositories;

/// <summary>
/// Books are children of the <see cref="Author"/> aggregate: reads are projections,
/// writes always load the owning Author and go through its methods.
/// </summary>
public class BooksRepository(AppDbContext dbContext) : IBooks
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IEnumerable<BookDTO>> GetBooks()
    {
        return await BookQuery().ToListAsync();
    }

    public async Task<IEnumerable<BookDTO>> GetAuthorBooks(int id)
    {
        return await BookQuery().Where(b => b.AuthorId == id).ToListAsync();
    }

    public async Task<BookDTO?> GetBook(int id)
    {
        return await BookQuery().Where(b => b.Id == id).FirstOrDefaultAsync();
    }

    public async Task<GeneralResponse> AddBook(BookDTO book)
    {
        Author? author = await LoadAuthor(book.AuthorId);

        if (author is null)
            return new(false, "Author not found.");

        try
        {
            author.AddBook(book.Title);
        }
        catch (DomainException ex)
        {
            return new(false, ex.Message);
        }

        int rowsAffected = await _dbContext.SaveChangesAsync();

        return rowsAffected > 0
            ? new(true, "Book added successfully.")
            : new(false, "Book could not be added.");
    }

    public async Task<GeneralResponse> UpdateBook(int id, BookDTO book)
    {
        Author? owner = await LoadOwner(id);

        if (owner is null)
            return new(false, "Book not found.");

        try
        {
            if (owner.Id == book.AuthorId)
            {
                owner.UpdateBook(id, book.Title);
            }
            else
            {
                Author? target = await LoadAuthor(book.AuthorId);

                if (target is null)
                    return new(false, "Author not found.");

                owner.TransferBook(id, target, book.Title);
            }
        }
        catch (DomainException ex)
        {
            return new(false, ex.Message);
        }

        int rowsAffected = await _dbContext.SaveChangesAsync();

        return rowsAffected > 0
            ? new(true, "Book updated successfully.")
            : new(false, "Book could not be updated.");
    }

    public async Task<GeneralResponse> DeleteBook(int id)
    {
        Author? owner = await LoadOwner(id);

        if (owner is null)
            return new(false, "Book not found.");

        owner.RemoveBook(id);

        int rowsAffected = await _dbContext.SaveChangesAsync();

        if (rowsAffected <= 0)
            throw new InvalidOperationException("Book could not be deleted.");

        return new(true, "Book deleted successfully.");
    }

    private IQueryable<BookDTO> BookQuery() =>
        _dbContext.Authors
            .AsNoTracking()
            .SelectMany(a => a.Books, (a, b) => new BookDTO
            {
                Id = b.Id,
                Title = b.Title,
                AuthorName = a.Name,
                AuthorId = a.Id
            });

    private Task<Author?> LoadAuthor(int id) =>
        _dbContext.Authors.Include(a => a.Books).FirstOrDefaultAsync(a => a.Id == id);

    private Task<Author?> LoadOwner(int bookId) =>
        _dbContext.Authors.Include(a => a.Books)
            .FirstOrDefaultAsync(a => a.Books.Any(b => b.Id == bookId));
}
