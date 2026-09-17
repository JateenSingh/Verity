namespace Verity.Domain.Entities;

public sealed class Like
{
    public Guid PostId { get; private set; }
    public Post Post { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Like()
    {
    }

    public static Like Create(Guid postId, Guid userId, DateTimeOffset? createdAt = null)
    {
        return new Like
        {
            PostId = postId,
            UserId = userId,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };
    }
}
