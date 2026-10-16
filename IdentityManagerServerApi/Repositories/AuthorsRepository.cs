using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Entities;
using IdentityManagerServerApi.Exceptions;
using Microsoft.EntityFrameworkCore;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using GeneralResponse = SharedClassLibrary.Models.ServiceResponses.GeneralResponse;

namespace IdentityManagerServerApi.Repositories;

public class AuthorsRepository(AppDbContext dbContext) : IAuthors
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<AuthorDTO?> GetAuthor(int id)
    {
        return await _dbContext.Authors
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AuthorDTO
            {
                Id = a.Id,
                Name = a.Name
            })
            .FirstOrDefaultAsync();
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
        Author newAuthor;

        try
        {
            newAuthor = Author.Create(author.Name);
        }
        catch (DomainException ex)
        {
            return new(false, ex.Message);
        }

        _dbContext.Authors.Add(newAuthor);
        int rowsAffected = await _dbContext.SaveChangesAsync();

        return rowsAffected > 0
            ? new(true, "Author added successfully.")
            : new(false, "Author was not added.");
    }

    public async Task<GeneralResponse> UpdateAuthor(int id, AuthorDTO author)
    {
        Author? existingAuthor = await LoadAggregate(id);

        if (existingAuthor is null)
            return new(false, "Author not found.");

        try
        {
            existingAuthor.Rename(author.Name);
        }
        catch (DomainException ex)
        {
            return new(false, ex.Message);
        }

        int rowsAffected = await _dbContext.SaveChangesAsync();

        return rowsAffected > 0
            ? new(true, "Author updated successfully.")
            : new(false, "Author was not updated.");
    }

    public async Task<GeneralResponse> DeleteAuthor(int id)
    {
        Author? author = await LoadAggregate(id);

        if (author is null)
            return new(false, "Author not found.");

        try
        {
            author.EnsureCanBeDeleted();
        }
        catch (DomainException ex)
        {
            return new(false, ex.Message);
        }

        _dbContext.Authors.Remove(author);
        int rowsAffected = await _dbContext.SaveChangesAsync();

        return rowsAffected > 0
            ? new(true, "Author deleted successfully.")
            : new(false, "Author was not deleted.");
    }

    private Task<Author?> LoadAggregate(int id) =>
        _dbContext.Authors.Include(a => a.Books).FirstOrDefaultAsync(a => a.Id == id);
}
