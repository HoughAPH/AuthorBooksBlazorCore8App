using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagerServerApi.Repositories;

public class AuthorRepository(AppDbContext dbContext) : IAuthorRepository
{
    public Task<Author?> GetById(int id) =>
        dbContext.Authors.Include(a => a.Books).FirstOrDefaultAsync(a => a.Id == id);

    public Task<Author?> GetByBookId(int bookId) =>
        dbContext.Authors.Include(a => a.Books)
            .FirstOrDefaultAsync(a => a.Books.Any(b => b.Id == bookId));

    public void Add(Author author) => dbContext.Authors.Add(author);

    public void Remove(Author author) => dbContext.Authors.Remove(author);

    public Task<int> SaveChanges() => dbContext.SaveChangesAsync();
}
