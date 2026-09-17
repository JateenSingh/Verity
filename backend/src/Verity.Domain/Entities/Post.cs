namespace Verity.Domain.Entities;

public sealed class Post
{
    private const int TitleMinLength = 3;
    private const int TitleMaxLength = 200;
    private const int BodyMaxLength = 10000;

    private readonly List<Comment> _comments = [];
    private readonly List<Like> _likes = [];
    private readonly List<PostTagAssignment> _tags = [];

    public Guid Id { get; private set; }
    public Guid AuthorId { get; private set; }
    public User Author { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public int LikeCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<Comment> Comments => _comments;
    public IReadOnlyCollection<Like> Likes => _likes;
    public IReadOnlyCollection<PostTagAssignment> Tags => _tags;

    private Post()
    {
    }

    public static Post Create(Guid authorId, string title, string body, Guid? id = null, DateTimeOffset? createdAt = null)
    {
        title = title.Trim();

        if (title.Length < TitleMinLength || title.Length > TitleMaxLength)
        {
            throw new DomainException(DomainErrorCode.InvalidTitle, $"Title must be between {TitleMinLength} and {TitleMaxLength} characters.");
        }

        if (string.IsNullOrEmpty(body) || body.Length > BodyMaxLength)
        {
            throw new DomainException(DomainErrorCode.InvalidBody, $"Body must be between 1 and {BodyMaxLength} characters.");
        }

        return new Post
        {
            Id = id ?? Guid.CreateVersion7(),
            AuthorId = authorId,
            Title = title,
            Body = body,
            LikeCount = 0,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };
    }

    public void EnsureCanBeLikedBy(Guid userId)
    {
        if (userId == AuthorId)
        {
            throw new DomainException(DomainErrorCode.SelfLikeNotAllowed, "You cannot like your own post.");
        }
    }
}
