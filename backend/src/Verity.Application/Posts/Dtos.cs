using System.ComponentModel.DataAnnotations;
using Verity.Application.Auth;

namespace Verity.Application.Posts;

public sealed record PostTagDto(string Tag, UserSummaryDto TaggedBy, string? Reason, DateTimeOffset CreatedAt);

public sealed record PostSummaryDto(
    Guid Id,
    string Title,
    string Excerpt,
    UserSummaryDto Author,
    DateTimeOffset CreatedAt,
    int LikeCount,
    int CommentCount,
    IReadOnlyList<PostTagDto> Tags,
    bool ViewerHasLiked);

public sealed record PostDetailDto(
    Guid Id,
    string Title,
    string Body,
    UserSummaryDto Author,
    DateTimeOffset CreatedAt,
    int LikeCount,
    int CommentCount,
    IReadOnlyList<PostTagDto> Tags,
    bool ViewerHasLiked);

public sealed record CreatePostRequest(
    [Required, MinLength(3), MaxLength(200)] string Title,
    [Required, MaxLength(10000)] string Body);
