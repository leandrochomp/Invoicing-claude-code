using Microsoft.EntityFrameworkCore;

namespace Shared.Data;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalRecords, int TotalPages);

public static class PagingExtensions
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    // Pages an already-ordered query. Page is clamped to >= 1; a page size outside
    // 1..MaxPageSize falls back to DefaultPageSize.
    public static async Task<PagedResponse<T>> ToPagedAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? DefaultPageSize : pageSize;

        var totalRecords = await query.CountAsync(cancellationToken);
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<T>(items, page, pageSize, totalRecords, totalPages);
    }
}
