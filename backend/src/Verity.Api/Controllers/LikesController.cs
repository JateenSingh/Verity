using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Verity.Application.Abstractions;
using Verity.Application.Likes;

namespace Verity.Api.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/posts/{postId:guid}/likes")]
public sealed class LikesController(LikeService likeService, ICurrentUser currentUser) : ControllerBase
{
    [HttpPut("me")]
    public async Task<ActionResult<LikeStatusDto>> Like(Guid postId, CancellationToken cancellationToken)
    {
        var status = await likeService.LikeAsync(postId, currentUser.UserId!.Value, cancellationToken);
        return status is null ? NotFound() : StatusCode(StatusCodes.Status201Created, status);
    }

    [HttpDelete("me")]
    public async Task<ActionResult<LikeStatusDto>> Unlike(Guid postId, CancellationToken cancellationToken)
    {
        var status = await likeService.UnlikeAsync(postId, currentUser.UserId!.Value, cancellationToken);
        return status is null ? NotFound() : Ok(status);
    }
}
