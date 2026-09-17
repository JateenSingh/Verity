using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Verity.Application.Abstractions;
using Verity.Application.Posts;
using Verity.Application.Tags;
using Verity.Domain.Enums;

namespace Verity.Api.Controllers;

[ApiController]
[Authorize(Policy = "ModeratorOnly")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/posts/{postId:guid}/tags")]
public sealed class TagsController(TagService tagService, ICurrentUser currentUser) : ControllerBase
{
    [HttpPut("{tag}")]
    public async Task<ActionResult<PostDetailDto>> Tag(Guid postId, PostTag tag, [FromBody] TagRequest? request, CancellationToken cancellationToken)
    {
        var post = await tagService.TagAsync(postId, tag, currentUser.UserId!.Value, request?.Reason, cancellationToken);
        return post is null ? NotFound() : StatusCode(StatusCodes.Status201Created, post);
    }

    [HttpDelete("{tag}")]
    public async Task<IActionResult> Untag(Guid postId, PostTag tag, CancellationToken cancellationToken)
    {
        var removed = await tagService.RemoveTagAsync(postId, tag, cancellationToken);
        return removed ? NoContent() : NotFound();
    }
}
