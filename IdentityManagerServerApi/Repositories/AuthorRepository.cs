using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagerServerApi.Repositories;

public class AuthorRepository(AppDbContext dbContext) : IAuthorRepository
{
    // SQL Server: 2601 = duplicate key in a unique index, 2627 = unique constraint violation.
    private static readonly int[] SqlServerUniqueViolations = [2601, 2627];

    public Task<Author?> GetById(int id) =>
        dbContext.Authors.Include(a => a.Books).FirstOrDefaultAsync(a => a.Id == id);

    public Task<Author?> GetByBookId(int bookId) =>
        dbContext.Authors.Include(a => a.Books)
            .FirstOrDefaultAsync(a => a.Books.Any(b => b.Id == bookId));

    public void Add(Author author) => dbContext.Authors.Add(author);

    public void Remove(Author author) => dbContext.Authors.Remove(author);

    /// <summary>
    /// Saves the aggregate. The domain already rejects duplicate titles, but two concurrent requests can both pass
    /// that in-memory check; the unique (AuthorId, Title) index then rejects the second, and it is reported as the
    /// same conflict the domain would have raised.
    /// </summary>
    public async Task<int> SaveChanges()
    {
        try
        {
            return await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new DomainException("This author already has a book with that title.", DomainErrorKind.Conflict);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql
            ? SqlServerUniqueViolations.Contains(sql.Number)
            : ex.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true; // SQLite (tests)
}
