using Verity.Domain.Entities;

namespace Verity.Application.Abstractions;

public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    IssuedToken IssueToken(User user);
}
