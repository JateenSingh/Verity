using Verity.Domain.Entities;

namespace Verity.Domain.Tests;

public class PostTests
{
    private static readonly Guid AuthorId = Guid.NewGuid();

    [Fact]
    public void EnsureCanBeLikedBy_throws_for_author()
    {
        var post = Post.Create(AuthorId, "A valid title", "A valid body");

        var exception = Assert.Throws<DomainException>(() => post.EnsureCanBeLikedBy(AuthorId));

        Assert.Equal(DomainErrorCode.SelfLikeNotAllowed, exception.Code);
    }

    [Fact]
    public void EnsureCanBeLikedBy_allows_other_user()
    {
        var post = Post.Create(AuthorId, "A valid title", "A valid body");

        var exception = Record.Exception(() => post.EnsureCanBeLikedBy(Guid.NewGuid()));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("ab")] // 2 chars: below the 3-char minimum
    public void Create_rejects_short_title(string title)
    {
        var exception = Assert.Throws<DomainException>(() => Post.Create(AuthorId, title, "body"));
        Assert.Equal(DomainErrorCode.InvalidTitle, exception.Code);
    }

    [Fact]
    public void Create_rejects_long_title()
    {
        var title = new string('a', 201); // 201 chars: above the 200-char maximum

        var exception = Assert.Throws<DomainException>(() => Post.Create(AuthorId, title, "body"));

        Assert.Equal(DomainErrorCode.InvalidTitle, exception.Code);
    }

    [Fact]
    public void Create_accepts_minimum_title_length()
    {
        var exception = Record.Exception(() => Post.Create(AuthorId, "abc", "body")); // 3 chars: exactly the minimum

        Assert.Null(exception);
    }

    [Fact]
    public void Create_accepts_maximum_title_length()
    {
        var title = new string('a', 200); // exactly the 200-char maximum

        var exception = Record.Exception(() => Post.Create(AuthorId, title, "body"));

        Assert.Null(exception);
    }

    [Fact]
    public void Create_trims_title()
    {
        var post = Post.Create(AuthorId, "  Leading and trailing whitespace  ", "body");

        Assert.Equal("Leading and trailing whitespace", post.Title);
    }

    [Fact]
    public void Create_rejects_empty_body()
    {
        var exception = Assert.Throws<DomainException>(() => Post.Create(AuthorId, "A valid title", ""));

        Assert.Equal(DomainErrorCode.InvalidBody, exception.Code);
    }
}
