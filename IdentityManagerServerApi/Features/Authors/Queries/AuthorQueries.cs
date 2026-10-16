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
            .Select(a => new AuthorDTO { Id = a.Id, Name = a.Name })
            .ToListAsync();
}

public class GetAuthorHandler(AppDbContext dbContext) : IQueryHandler<GetAuthorQuery, AuthorDTO?>
{
    public Task<AuthorDTO?> Handle(GetAuthorQuery query) =>
        dbContext.Authors
            .AsNoTracking()
            .Where(a => a.Id == query.Id)
            .Select(a => new AuthorDTO { Id = a.Id, Name = a.Name })
            .FirstOrDefaultAsync();
}
