using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Verity.Application.Abstractions;
using Verity.Application.Comments;
using Verity.Application.Common;
using Verity.Application.Posts;

namespace Verity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/posts/{postId:guid}/comments")]
public sealed class CommentsController(CommentService commentService, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<CommentDto>>> List(
        Guid postId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] SortDirection? sortDirection,
        CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedPageSize) = PagingDefaults.Normalize(page, pageSize);
        var query = new CommentListQuery
        {
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            SortDirection = sortDirection ?? SortDirection.Asc,
        };

        var result = await commentService.ListAsync(postId, query, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CommentDto>> Create(Guid postId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var comment = await commentService.CreateAsync(
            postId,
            currentUser.UserId!.Value,
            currentUser.Username!,
            currentUser.Role!,
            request,
            cancellationToken);

        return comment is null ? NotFound() : CreatedAtAction(nameof(List), new { postId }, comment);
    }
}
