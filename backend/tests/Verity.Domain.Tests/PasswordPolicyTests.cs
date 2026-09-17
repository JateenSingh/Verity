using Verity.Application.Auth;

namespace Verity.Domain.Tests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("short1")] // below 8 chars
    [InlineData("noDigitsHere")] // no digit
    [InlineData("12345678")] // no letter
    [InlineData("")]
    public void Rejects_weak_passwords(string password)
    {
        Assert.False(PasswordPolicy.IsSatisfiedBy(password));
    }

    [Fact]
    public void Rejects_password_over_max_length()
    {
        var password = new string('a', 121) + "1"; // 122 chars, one digit, over the 128 cap once combined below

        Assert.True(password.Length <= PasswordPolicy.MaxLength); // sanity check on the fixture itself
        Assert.True(PasswordPolicy.IsSatisfiedBy(password));

        var tooLong = new string('a', 128) + "1"; // 129 chars: over the max
        Assert.False(PasswordPolicy.IsSatisfiedBy(tooLong));
    }

    [Fact]
    public void Accepts_valid_password()
    {
        Assert.True(PasswordPolicy.IsSatisfiedBy("TestOnly_Policy1!"));
    }
}
