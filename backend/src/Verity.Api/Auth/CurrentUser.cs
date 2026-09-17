using Verity.Application.Abstractions;
using Verity.Domain.Enums;

namespace Verity.Api.Auth;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Username => httpContextAccessor.HttpContext?.User.FindFirst("name")?.Value;

    public string? Role => httpContextAccessor.HttpContext?.User.FindFirst("role")?.Value;

    public bool IsModerator => Role == nameof(UserRole.Moderator);
}
