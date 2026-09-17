using Verity.Domain.Entities;

namespace Verity.Domain.Tests;

public class CommentTests
{
    private static readonly Guid PostId = Guid.NewGuid();
    private static readonly Guid AuthorId = Guid.NewGuid();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_body(string body)
    {
        var exception = Assert.Throws<DomainException>(() => Comment.Create(PostId, AuthorId, body));

        Assert.Equal(DomainErrorCode.InvalidComment, exception.Code);
    }

    [Fact]
    public void Create_rejects_body_over_max_length()
    {
        var body = new string('a', 4001);

        var exception = Assert.Throws<DomainException>(() => Comment.Create(PostId, AuthorId, body));

        Assert.Equal(DomainErrorCode.InvalidComment, exception.Code);
    }

    [Fact]
    public void Create_accepts_valid_body()
    {
        var comment = Comment.Create(PostId, AuthorId, "A perfectly reasonable comment.");

        Assert.Equal("A perfectly reasonable comment.", comment.Body);
    }
}
