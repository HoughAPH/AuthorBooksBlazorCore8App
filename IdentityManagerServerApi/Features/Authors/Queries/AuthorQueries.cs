using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Features.Common;
using Microsoft.EntityFrameworkCore;
using SharedClassLibrary.DTOs;

namespace IdentityManagerServerApi.Features.Authors.Queries;

public record GetAuthorsQuery;
public record GetAuthorQuery(int Id);

public class GetAuthorsHandler(AppDbContext dbContext) : IQueryHandler<GetAuthorsQuery, List<AuthorDTO>>
{
    public Task<List<AuthorDTO>> Handle(GetAuthorsQuery query) =>
        dbContext.Authors
            .AsNoTracking()
            .Select(a => new AuthorDTO { Id = a.Id, Name = a.Name, Version = a.Version })
            .ToListAsync();
}

public class GetAuthorHandler(AppDbContext dbContext) : IQueryHandler<GetAuthorQuery, AuthorDTO?>
{
    public Task<AuthorDTO?> Handle(GetAuthorQuery query) =>
        dbContext.Authors
            .AsNoTracking()
            .Where(a => a.Id == query.Id)
            .Select(a => new AuthorDTO { Id = a.Id, Name = a.Name, Version = a.Version })
            .FirstOrDefaultAsync();
}

public record GetAuthorsPageQuery(int PageNumber, int PageSize, string? Search);

public class GetAuthorsPageHandler(AppDbContext dbContext) : IQueryHandler<GetAuthorsPageQuery, PagedResult<AuthorDTO>>
{
    public Task<PagedResult<AuthorDTO>> Handle(GetAuthorsPageQuery query)
    {
        IQueryable<Domain.Author> authors = dbContext.Authors.AsNoTracking();

        if (Paging.Term(query.Search) is { } term)
            authors = authors.Where(a => a.Name.ToLower().Contains(term));

        return Paging.ToPage(
            authors
                .OrderBy(a => a.Name).ThenBy(a => a.Id)
                .Select(a => new AuthorDTO { Id = a.Id, Name = a.Name, Version = a.Version }),
            query.PageNumber,
            query.PageSize);
    }
}
