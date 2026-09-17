using Verity.Domain.Enums;

namespace Verity.Domain.Entities;

public sealed class PostTagAssignment
{
    private const int ReasonMaxLength = 500;

    public Guid PostId { get; private set; }
    public Post Post { get; private set; } = null!;
    public PostTag Tag { get; private set; }
    public Guid TaggedByUserId { get; private set; }
    public User TaggedBy { get; private set; } = null!;
    public string? Reason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private PostTagAssignment()
    {
    }

    public static PostTagAssignment Create(Guid postId, PostTag tag, Guid taggedByUserId, string? reason, DateTimeOffset? createdAt = null)
    {
        reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        if (reason is not null && reason.Length > ReasonMaxLength)
        {
            throw new DomainException(DomainErrorCode.InvalidReason, $"Reason must be at most {ReasonMaxLength} characters.");
        }

        return new PostTagAssignment
        {
            PostId = postId,
            Tag = tag,
            TaggedByUserId = taggedByUserId,
            Reason = reason,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };
    }
}
