using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Verity.Application.Abstractions;
using Verity.Application.Auth;
using Verity.Application.Data;

namespace Verity.Api.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController(IVerityDbContext db, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserSummaryDto>> Me(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserSummaryDto(u.Id, u.Username, u.Role.ToString()))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }
}
