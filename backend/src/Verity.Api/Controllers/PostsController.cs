using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Verity.Application.Abstractions;
using Verity.Application.Common;
using Verity.Application.Posts;
using Verity.Domain.Enums;

namespace Verity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/posts")]
public sealed class PostsController(PostService postService, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<PostSummaryDto>>> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] string? author,
        [FromQuery] Guid? authorId,
        [FromQuery] PostTag? tag,
        [FromQuery] bool untagged,
        [FromQuery] PostSortBy? sortBy,
        [FromQuery] SortDirection? sortDirection,
        CancellationToken cancellationToken)
    {
        if (dateFrom is not null && dateTo is not null && dateFrom > dateTo)
        {
            ModelState.AddModelError(nameof(dateFrom), "dateFrom must not be after dateTo.");
        }

        if (untagged && tag is not null)
        {
            ModelState.AddModelError(nameof(tag), "tag and untagged cannot both be set.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (normalizedPage, normalizedPageSize) = PagingDefaults.Normalize(page, pageSize);
        var query = new PostListQuery
        {
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            // Model binding parses a date-only or offset-less query value
            // using the server's local offset (e.g. a date like "2026-01-01"
            // becomes midnight +02:00 on a machine in that zone), but Npgsql
            // rejects any DateTimeOffset for a timestamptz column that isn't
            // already UTC. Normalizing here keeps that a controller-layer
            // concern instead of leaking into the query/persistence layers.
            DateFrom = dateFrom?.ToUniversalTime(),
            DateTo = dateTo?.ToUniversalTime(),
            Author = author,
            AuthorId = authorId,
            Tag = tag,
            Untagged = untagged,
            SortBy = sortBy ?? PostSortBy.CreatedAt,
            SortDirection = sortDirection ?? SortDirection.Desc,
        };

        var result = await postService.ListAsync(query, currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PostDetailDto>> Create(CreatePostRequest request, CancellationToken cancellationToken)
    {
        var post = await postService.CreateAsync(
            currentUser.UserId!.Value,
            currentUser.Username!,
            currentUser.Role!,
            request,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { postId = post.Id }, post);
    }

    [HttpGet("{postId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PostDetailDto>> GetById(Guid postId, CancellationToken cancellationToken)
    {
        var post = await postService.GetByIdAsync(postId, currentUser.UserId, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }
}
