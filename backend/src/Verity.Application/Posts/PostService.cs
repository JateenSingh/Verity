using Microsoft.EntityFrameworkCore;
using Verity.Application.Abstractions;
using Verity.Application.Auth;
using Verity.Application.Common;
using Verity.Application.Data;
using Verity.Domain.Entities;

namespace Verity.Application.Posts;

public sealed class PostService(IVerityDbContext db, IClock clock)
{
    public async Task<PagedResult<PostSummaryDto>> ListAsync(PostListQuery query, Guid? viewerId, CancellationToken ct)
    {
        IQueryable<Post> filtered = db.Posts.AsNoTracking();

        if (query.DateFrom is not null)
        {
            filtered = filtered.Where(p => p.CreatedAt >= query.DateFrom);
        }

        if (query.DateTo is not null)
        {
            filtered = filtered.Where(p => p.CreatedAt <= query.DateTo);
        }

        if (query.AuthorId is not null)
        {
            filtered = filtered.Where(p => p.AuthorId == query.AuthorId);
        }

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            var authorLower = query.Author.Trim().ToLowerInvariant();
            filtered = filtered.Where(p => EF.Property<string>(p.Author, "UsernameLower") == authorLower);
        }

        if (query.Untagged)
        {
            filtered = filtered.Where(p => !p.Tags.Any());
        }
        else if (query.Tag is not null)
        {
            filtered = filtered.Where(p => p.Tags.Any(t => t.Tag == query.Tag));
        }

        var totalCount = await filtered.CountAsync(ct);

        // "flip for asc": both the primary sort key and the Id tie-break flip
        // together, so pages stay stable in either direction.
        IQueryable<Post> ordered = (query.SortBy, query.SortDirection) switch
        {
            (PostSortBy.LikeCount, SortDirection.Asc) => filtered.OrderBy(p => p.LikeCount).ThenBy(p => p.CreatedAt).ThenBy(p => p.Id),
            (PostSortBy.LikeCount, SortDirection.Desc) => filtered.OrderByDescending(p => p.LikeCount).ThenByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id),
            (PostSortBy.CreatedAt, SortDirection.Asc) => filtered.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            _ => filtered.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id),
        };

        var page = ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize);
        var items = await ProjectSummary(page, viewerId).ToListAsync(ct);

        return PagedResult<PostSummaryDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<PostDetailDto> CreateAsync(Guid authorId, string authorUsername, string authorRole, CreatePostRequest request, CancellationToken ct)
    {
        var post = Post.Create(authorId, request.Title, request.Body, createdAt: clock.UtcNow);

        db.Posts.Add(post);
        await db.SaveChangesAsync(ct);

        return new PostDetailDto(
            post.Id,
            post.Title,
            post.Body,
            new UserSummaryDto(authorId, authorUsername, authorRole),
            post.CreatedAt,
            LikeCount: 0,
            CommentCount: 0,
            Tags: [],
            ViewerHasLiked: false);
    }

    public Task<PostDetailDto?> GetByIdAsync(Guid postId, Guid? viewerId, CancellationToken ct) =>
        ProjectDetail(db.Posts.AsNoTracking().Where(p => p.Id == postId), viewerId).FirstOrDefaultAsync(ct);

    internal static IQueryable<PostSummaryDto> ProjectSummary(IQueryable<Post> query, Guid? viewerId) =>
        query.Select(p => new PostSummaryDto(
            p.Id,
            p.Title,
            p.Body.Length <= 200 ? p.Body : p.Body.Substring(0, 200),
            new UserSummaryDto(p.Author.Id, p.Author.Username, p.Author.Role.ToString()),
            p.CreatedAt,
            p.LikeCount,
            p.Comments.Count(),
            p.Tags.Select(t => new PostTagDto(
                t.Tag.ToString(),
                new UserSummaryDto(t.TaggedBy.Id, t.TaggedBy.Username, t.TaggedBy.Role.ToString()),
                t.Reason,
                t.CreatedAt)).ToList(),
            viewerId != null && p.Likes.Any(l => l.UserId == viewerId)));

    internal static IQueryable<PostDetailDto> ProjectDetail(IQueryable<Post> query, Guid? viewerId) =>
        query.Select(p => new PostDetailDto(
            p.Id,
            p.Title,
            p.Body,
            new UserSummaryDto(p.Author.Id, p.Author.Username, p.Author.Role.ToString()),
            p.CreatedAt,
            p.LikeCount,
            p.Comments.Count(),
            p.Tags.Select(t => new PostTagDto(
                t.Tag.ToString(),
                new UserSummaryDto(t.TaggedBy.Id, t.TaggedBy.Username, t.TaggedBy.Role.ToString()),
                t.Reason,
                t.CreatedAt)).ToList(),
            viewerId != null && p.Likes.Any(l => l.UserId == viewerId)));
}
