namespace Verity.Domain.Entities;

public sealed class Comment
{
    private const int BodyMaxLength = 4000;

    public Guid Id { get; private set; }
    public Guid PostId { get; private set; }
    public Post Post { get; private set; } = null!;
    public Guid AuthorId { get; private set; }
    public User Author { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Comment()
    {
    }

    public static Comment Create(Guid postId, Guid authorId, string body, Guid? id = null, DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Length > BodyMaxLength)
        {
            throw new DomainException(DomainErrorCode.InvalidComment, $"Comment must be between 1 and {BodyMaxLength} characters.");
        }

        return new Comment
        {
            Id = id ?? Guid.CreateVersion7(),
            PostId = postId,
            AuthorId = authorId,
            Body = body,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };
    }
}
