namespace Verity.Application.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<T>(items, page, pageSize, totalCount, totalPages);
    }
}
