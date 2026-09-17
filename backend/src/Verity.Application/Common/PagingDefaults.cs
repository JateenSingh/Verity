namespace Verity.Application.Common;

public static class PagingDefaults
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
    {
        var normalizedPage = page is null or < 1 ? 1 : page.Value;
        var normalizedPageSize = pageSize switch
        {
            null or < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize.Value,
        };

        return (normalizedPage, normalizedPageSize);
    }
}
