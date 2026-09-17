using System.ComponentModel.DataAnnotations;
using Verity.Application.Auth;
using Verity.Application.Posts;

namespace Verity.Application.Comments;

public sealed record CommentDto(Guid Id, Guid PostId, string Body, UserSummaryDto Author, DateTimeOffset CreatedAt);

public sealed record CreateCommentRequest([Required, MaxLength(4000)] string Body);

public sealed record CommentListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public SortDirection SortDirection { get; init; } = SortDirection.Asc;
}
