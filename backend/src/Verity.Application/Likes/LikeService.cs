using Microsoft.EntityFrameworkCore;
using Verity.Application.Abstractions;
using Verity.Application.Data;
using Verity.Domain;
using Verity.Domain.Entities;

namespace Verity.Application.Likes;

public sealed class LikeService(IVerityDbContext db, IClock clock)
{
    public async Task<LikeStatusDto?> LikeAsync(Guid postId, Guid userId, CancellationToken ct)
    {
        // Only the author id is fetched - no need to load the full Post just
        // to check the no-self-like rule.
        var post = await db.Posts
            .Where(p => p.Id == postId)
            .Select(p => new { p.AuthorId })
            .FirstOrDefaultAsync(ct);

        if (post is null)
        {
            return null;
        }

        if (post.AuthorId == userId)
        {
            throw new DomainException(DomainErrorCode.SelfLikeNotAllowed, "You cannot like your own post.");
        }

        // A unique-constraint violation here (duplicate like) propagates as a
        // DbUpdateException before the counter below ever runs, and the
        // transaction rolls back - the global exception handler turns it
        // into a 409.
        await db.ExecuteInTransactionAsync(async cancellationToken =>
        {
            db.Likes.Add(Like.Create(postId, userId, clock.UtcNow));
            await db.SaveChangesAsync(cancellationToken);

            await db.Posts
                .Where(p => p.Id == postId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.LikeCount, p => p.LikeCount + 1), cancellationToken);
        }, ct);

        var likeCount = await db.Posts.Where(p => p.Id == postId).Select(p => p.LikeCount).FirstAsync(ct);
        return new LikeStatusDto(postId, likeCount, true);
    }

    public async Task<LikeStatusDto?> UnlikeAsync(Guid postId, Guid userId, CancellationToken ct)
    {
        var deleted = await db.Likes
            .Where(l => l.PostId == postId && l.UserId == userId)
            .ExecuteDeleteAsync(ct);

        if (deleted == 0)
        {
            return null;
        }

        await db.Posts
            .Where(p => p.Id == postId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.LikeCount, p => p.LikeCount > 0 ? p.LikeCount - 1 : 0), ct);

        var likeCount = await db.Posts.Where(p => p.Id == postId).Select(p => p.LikeCount).FirstAsync(ct);
        return new LikeStatusDto(postId, likeCount, false);
    }
}
