namespace Verity.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Username { get; }

    string? Role { get; }

    bool IsModerator { get; }
}
