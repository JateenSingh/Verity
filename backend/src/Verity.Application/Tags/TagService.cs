using Microsoft.EntityFrameworkCore;
using Verity.Application.Abstractions;
using Verity.Application.Data;
using Verity.Application.Posts;
using Verity.Domain.Entities;
using Verity.Domain.Enums;

namespace Verity.Application.Tags;

public sealed class TagService(IVerityDbContext db, IClock clock)
{
    public async Task<PostDetailDto?> TagAsync(Guid postId, PostTag tag, Guid taggedByUserId, string? reason, CancellationToken ct)
    {
        var postExists = await db.Posts.AnyAsync(p => p.Id == postId, ct);
        if (!postExists)
        {
            return null;
        }

        // Duplicate PK on (post_id, tag) -> DbUpdateException, mapped globally to 409.
        db.PostTags.Add(PostTagAssignment.Create(postId, tag, taggedByUserId, reason, clock.UtcNow));
        await db.SaveChangesAsync(ct);

        return await PostService.ProjectDetail(db.Posts.AsNoTracking().Where(p => p.Id == postId), viewerId: null).FirstAsync(ct);
    }

    public async Task<bool> RemoveTagAsync(Guid postId, PostTag tag, CancellationToken ct)
    {
        var deleted = await db.PostTags
            .Where(t => t.PostId == postId && t.Tag == tag)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
