using Microsoft.EntityFrameworkCore;
using SharedClassLibrary.DTOs;

namespace IdentityManagerServerApi.Features.Common;

/// <summary>Shared rules for the paged list queries: bounded page size, 1-based pages, stable ordering is the caller's job.</summary>
public static class Paging
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize) =>
        (Math.Max(pageNumber, 1), pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize));

    /// <summary>Normalised, lower-cased search term, or null when there is nothing to filter on.</summary>
    public static string? Term(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLowerInvariant();

    /// <summary>Counts, then reads one page of an already filtered and ordered query.</summary>
    public static async Task<PagedResult<T>> ToPage<T>(IQueryable<T> ordered, int pageNumber, int pageSize)
    {
        (int page, int size) = Normalize(pageNumber, pageSize);
        int total = await ordered.CountAsync();
        List<T> items = await ordered.Skip((page - 1) * size).Take(size).ToListAsync();

        return new PagedResult<T> { Items = items, PageNumber = page, PageSize = size, TotalCount = total };
    }
}
