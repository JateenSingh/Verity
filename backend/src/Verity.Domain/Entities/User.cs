using System.Text.RegularExpressions;
using Verity.Domain.Enums;

namespace Verity.Domain.Entities;

public sealed partial class User
{
    private const int UsernameMinLength = 3;
    private const int UsernameMaxLength = 32;
    private const int EmailMaxLength = 254;

    public Guid Id { get; private set; }
    public string Username { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsModerator => Role == UserRole.Moderator;

    private User()
    {
    }

    public static User Create(string username, string email, string passwordHash, UserRole role, Guid? id = null, DateTimeOffset? createdAt = null)
    {
        username = username.Trim();
        email = email.Trim();

        if (!UsernamePattern().IsMatch(username))
        {
            throw new DomainException(DomainErrorCode.InvalidUsername, $"Username must be {UsernameMinLength}-{UsernameMaxLength} characters of letters, digits or underscores.");
        }

        if (email.Length == 0 || email.Length > EmailMaxLength || !email.Contains('@'))
        {
            throw new DomainException(DomainErrorCode.InvalidEmail, $"Email must be a valid address of at most {EmailMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        return new User
        {
            Id = id ?? Guid.CreateVersion7(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };
    }

    public void UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    [GeneratedRegex("^[a-zA-Z0-9_]{3,32}$")]
    private static partial Regex UsernamePattern();
}
