using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Features.Common;
using Microsoft.EntityFrameworkCore;
using SharedClassLibrary.DTOs;

namespace IdentityManagerServerApi.Features.Books.Queries;

public record GetBooksQuery;
public record GetAuthorBooksQuery(int AuthorId);
public record GetBookQuery(int Id);

internal static class BookProjection
{
    // SelectMany with a result selector translates to a join; a nested Select would need APPLY, which SQLite lacks.
    internal static IQueryable<BookDTO> Books(AppDbContext dbContext) =>
        dbContext.Authors
            .AsNoTracking()
            .SelectMany(a => a.Books, (a, b) => new BookDTO
            {
                Id = b.Id,
                Title = b.Title,
                AuthorName = a.Name,
                AuthorId = a.Id
            });
}

public class GetBooksHandler(AppDbContext dbContext) : IQueryHandler<GetBooksQuery, IEnumerable<BookDTO>>
{
    public async Task<IEnumerable<BookDTO>> Handle(GetBooksQuery query) =>
        await BookProjection.Books(dbContext).ToListAsync();
}

public class GetAuthorBooksHandler(AppDbContext dbContext) : IQueryHandler<GetAuthorBooksQuery, IEnumerable<BookDTO>>
{
    public async Task<IEnumerable<BookDTO>> Handle(GetAuthorBooksQuery query) =>
        await BookProjection.Books(dbContext).Where(b => b.AuthorId == query.AuthorId).ToListAsync();
}

public class GetBookHandler(AppDbContext dbContext) : IQueryHandler<GetBookQuery, BookDTO?>
{
    public Task<BookDTO?> Handle(GetBookQuery query) =>
        BookProjection.Books(dbContext).Where(b => b.Id == query.Id).FirstOrDefaultAsync();
}

public record GetBooksPageQuery(int PageNumber, int PageSize, string? Search, int? AuthorId);

public class GetBooksPageHandler(AppDbContext dbContext) : IQueryHandler<GetBooksPageQuery, PagedResult<BookDTO>>
{
    public Task<PagedResult<BookDTO>> Handle(GetBooksPageQuery query)
    {
        IQueryable<BookDTO> books = BookProjection.Books(dbContext);

        if (query.AuthorId is { } authorId)
            books = books.Where(b => b.AuthorId == authorId);

        if (Paging.Term(query.Search) is { } term)
            books = books.Where(b => b.Title.ToLower().Contains(term) || b.AuthorName.ToLower().Contains(term));

        return Paging.ToPage(books.OrderBy(b => b.Title).ThenBy(b => b.Id), query.PageNumber, query.PageSize);
    }
}
