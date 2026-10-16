using IdentityManagerServerApi.Domain;

namespace IdentityManagerServerApi.Repositories;

/// <summary>Persistence for the Author aggregate. Aggregates are always loaded with their books.</summary>
public interface IAuthorRepository
{
    Task<Author?> GetById(int id);
    Task<Author?> GetByBookId(int bookId);
    void Add(Author author);
    void Remove(Author author);
    Task<int> SaveChanges();
}
