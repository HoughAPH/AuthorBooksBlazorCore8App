using IdentityManagerServerApi.Data;
using Microsoft.EntityFrameworkCore;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Models;
using GeneralResponse = SharedClassLibrary.Models.ServiceResponses.GeneralResponse;

namespace IdentityManagerServerApi.Repositories;

public class AuthorsRepository(AppDbContext dbContext) : IAuthors
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<AuthorDTO?> GetAuthor(int id)
    {
        Author? author = await _dbContext.Authors.FindAsync(id);

        if (author is null)
            return null;

        return new AuthorDTO
        {
            Id = author.Id,
            Name = author.Name
        };
    }

    public async Task<List<AuthorDTO>> GetAuthors()
    {
        List<AuthorDTO> authors = await _dbContext.Authors
            .Select(a => new AuthorDTO
            {
                Id = a.Id,
                Name = a.Name
            })
            .ToListAsync();

        return authors;
    }

    public async Task<GeneralResponse> AddAuthor(AuthorDTO author)
    {
        Author newAuthor = new()
        {
            Name = author.Name
        };

        _dbContext.Authors.Add(newAuthor);
        int rowsAffected = await _dbContext.SaveChangesAsync();

        return rowsAffected > 0
            ? new(true, "Author added successfully.")
            : new(false, "Author was not added.");
    }

    public async Task<GeneralResponse> UpdateAuthor(int id, AuthorDTO author)
    {
        Author? existingAuthor = await _dbContext.Authors.FindAsync(id);

        if (existingAuthor is null)
            return new(false, "Author not found.");

        existingAuthor.Name = author.Name;
        int rowsAffected = await _dbContext.SaveChangesAsync();

        return rowsAffected > 0
            ? new(true, "Author updated successfully.")
            : new(false, "Author was not updated.");
    }

    public async Task<GeneralResponse> DeleteAuthor(int id)
    {
        Author? author = await _dbContext.Authors.FindAsync(id);

        if (author is null)
            return new(false, "Author not found.");

        bool hasBooks = await _dbContext.Books.AnyAsync(b => b.AuthorId == id);
        if (hasBooks)
            return new(false, "Author cannot be deleted because the author has books.");

        _dbContext.Authors.Remove(author);
        int rowsAffected = await _dbContext.SaveChangesAsync();

        return rowsAffected > 0
            ? new(true, "Author deleted successfully.")
            : new(false, "Author was not deleted.");
    }
}
