using Verity.Domain.Entities;
using Verity.Domain.Enums;

namespace Verity.Domain.Tests;

public class UserTests
{
    [Theory]
    [InlineData("ab")] // 2 chars: below the 3-char minimum
    [InlineData("a-b")] // hyphen is not allowed
    [InlineData("has spaces")]
    [InlineData("")]
    public void Create_rejects_invalid_username(string username)
    {
        var exception = Assert.Throws<DomainException>(() =>
            User.Create(username, "person@example.com", "hash", UserRole.User));

        Assert.Equal(DomainErrorCode.InvalidUsername, exception.Code);
    }

    [Fact]
    public void Create_rejects_username_over_max_length()
    {
        var username = new string('a', 33);

        var exception = Assert.Throws<DomainException>(() =>
            User.Create(username, "person@example.com", "hash", UserRole.User));

        Assert.Equal(DomainErrorCode.InvalidUsername, exception.Code);
    }

    [Fact]
    public void Create_accepts_valid_username()
    {
        var user = User.Create("valid_user_123", "person@example.com", "hash", UserRole.User);

        Assert.Equal("valid_user_123", user.Username);
    }

    [Fact]
    public void IsModerator_reflects_role()
    {
        var moderator = User.Create("mod_user", "mod@example.com", "hash", UserRole.Moderator);
        var regular = User.Create("regular_user", "regular@example.com", "hash", UserRole.User);

        Assert.True(moderator.IsModerator);
        Assert.False(regular.IsModerator);
    }
}
