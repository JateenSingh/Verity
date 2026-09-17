using Microsoft.EntityFrameworkCore;
using Verity.Application.Abstractions;
using Verity.Application.Auth;
using Verity.Application.Common;
using Verity.Application.Data;
using Verity.Application.Posts;
using Verity.Domain.Entities;

namespace Verity.Application.Comments;

public sealed class CommentService(IVerityDbContext db, IClock clock)
{
    public async Task<CommentDto?> CreateAsync(Guid postId, Guid authorId, string authorUsername, string authorRole, CreateCommentRequest request, CancellationToken ct)
    {
        var postExists = await db.Posts.AnyAsync(p => p.Id == postId, ct);
        if (!postExists)
        {
            return null;
        }

        var comment = Comment.Create(postId, authorId, request.Body, createdAt: clock.UtcNow);
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        return new CommentDto(comment.Id, postId, comment.Body, new UserSummaryDto(authorId, authorUsername, authorRole), comment.CreatedAt);
    }

    public async Task<PagedResult<CommentDto>?> ListAsync(Guid postId, CommentListQuery query, CancellationToken ct)
    {
        var baseQuery = db.Comments.AsNoTracking().Where(c => c.PostId == postId);
        var totalCount = await baseQuery.CountAsync(ct);

        List<CommentDto> items;
        if (totalCount == 0)
        {
            items = [];
        }
        else
        {
            var ordered = query.SortDirection == SortDirection.Desc
                ? baseQuery.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
                : baseQuery.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id);

            items = await ordered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(c => new CommentDto(
                    c.Id,
                    c.PostId,
                    c.Body,
                    new UserSummaryDto(c.Author.Id, c.Author.Username, c.Author.Role.ToString()),
                    c.CreatedAt))
                .ToListAsync(ct);
        }

        // Only pay for an existence check when the page came back empty - the
        // common case (a post with comments) never needs this third query.
        if (items.Count == 0 && !await db.Posts.AnyAsync(p => p.Id == postId, ct))
        {
            return null;
        }

        return PagedResult<CommentDto>.Create(items, query.Page, query.PageSize, totalCount);
    }
}
