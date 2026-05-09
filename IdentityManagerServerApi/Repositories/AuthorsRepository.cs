using IdentityManagerServerApi.Data;
using Microsoft.EntityFrameworkCore;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Models;

namespace IdentityManagerServerApi.Repositories;

public class AuthorsRepository : IAuthors
{
    private readonly AppDbContext _dbContext;

    public AuthorsRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

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

    public async Task<int> AddAuthor(AuthorDTO author)
    {
        Author newAuthor = new()
        {
            Name = author.Name
        };

        _dbContext.Authors.Add(newAuthor);
        return await _dbContext.SaveChangesAsync();
    }

    public async Task<int> UpdateAuthor(int id, AuthorDTO author)
    {
        Author? existingAuthor = await _dbContext.Authors.FindAsync(id);

        if (existingAuthor is null)
            return 0;

        existingAuthor.Name = author.Name;
        return await _dbContext.SaveChangesAsync();
    }

    public async Task<string> DeleteAuthor(int id)
    {
        Author? author = await _dbContext.Authors.FindAsync(id);

        if (author is null)
            return "No Author found";

        if (await _dbContext.Books.AnyAsync(b => b.AuthorId == id))
            return "Author has books";

        _dbContext.Authors.Remove(author);
        int response = await _dbContext.SaveChangesAsync();
        return response > 0 ? "" : "Author not deleted";
    }
}
