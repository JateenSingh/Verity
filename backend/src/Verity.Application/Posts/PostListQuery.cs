using Verity.Domain.Enums;

namespace Verity.Application.Posts;

public enum PostSortBy
{
    CreatedAt,
    LikeCount,
}

public enum SortDirection
{
    Asc,
    Desc,
}

public sealed record PostListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo { get; init; }
    public string? Author { get; init; }
    public Guid? AuthorId { get; init; }
    public PostTag? Tag { get; init; }
    public bool Untagged { get; init; }
    public PostSortBy SortBy { get; init; } = PostSortBy.CreatedAt;
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;
}
