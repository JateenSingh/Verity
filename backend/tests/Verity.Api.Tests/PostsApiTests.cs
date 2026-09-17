using System.Net;
using System.Net.Http.Json;
using Verity.Api.Tests.Infrastructure;
using Verity.Application.Common;
using Verity.Application.Posts;

namespace Verity.Api.Tests;

[Collection("Api")]
public sealed class PostsApiTests(VerityApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_anonymous_returns_401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/posts", new CreatePostRequest("A title", "A body"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_authenticated_returns_201_with_location()
    {
        var (token, _) = await _client.RegisterUniqueUserAsync("postcreator");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts").WithBearerToken(token);
        request.Content = JsonContent.Create(new CreatePostRequest("A perfectly good title", "A perfectly good body"));
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task GetById_unknown_post_returns_404()
    {
        var response = await _client.GetAsync($"/api/v1/posts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_default_sort_is_createdAt_desc_and_stable_across_pages()
    {
        var page1 = await _client.GetFromJsonAsync<PagedResult<PostSummaryDto>>("/api/v1/posts?page=1&pageSize=10", TestHttpExtensions.JsonOptions);
        var page2 = await _client.GetFromJsonAsync<PagedResult<PostSummaryDto>>("/api/v1/posts?page=2&pageSize=10", TestHttpExtensions.JsonOptions);

        var combined = page1!.Items.Concat(page2!.Items).ToList();
        var sorted = combined.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).ToList();
        Assert.Equal(sorted.Select(p => p.Id), combined.Select(p => p.Id));

        var noDuplicates = combined.Select(p => p.Id).Distinct().Count();
        Assert.Equal(combined.Count, noDuplicates);
    }

    [Fact]
    public async Task List_dateFrom_after_dateTo_returns_400()
    {
        var response = await _client.GetAsync("/api/v1/posts?dateFrom=2026-06-01&dateTo=2020-01-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_author_filter_is_case_insensitive()
    {
        var lower = await _client.GetFromJsonAsync<PagedResult<PostSummaryDto>>("/api/v1/posts?author=jateen", TestHttpExtensions.JsonOptions);
        var upper = await _client.GetFromJsonAsync<PagedResult<PostSummaryDto>>("/api/v1/posts?author=JATEEN", TestHttpExtensions.JsonOptions);

        Assert.Equal(lower!.TotalCount, upper!.TotalCount);
        Assert.True(lower.TotalCount > 0);
    }

    [Fact]
    public async Task List_author_with_no_posts_returns_empty_with_zero_total()
    {
        var result = await _client.GetFromJsonAsync<PagedResult<PostSummaryDto>>("/api/v1/posts?author=naledi", TestHttpExtensions.JsonOptions);

        Assert.Empty(result!.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task List_tag_filter_returns_only_tagged_posts()
    {
        var result = await _client.GetFromJsonAsync<PagedResult<PostSummaryDto>>("/api/v1/posts?tag=MisleadingOrFalse&pageSize=100", TestHttpExtensions.JsonOptions);

        Assert.True(result!.TotalCount > 0);
        Assert.All(result.Items, p => Assert.Contains(p.Tags, t => t.Tag == "MisleadingOrFalse"));
    }

    [Fact]
    public async Task List_untagged_and_tag_together_returns_400()
    {
        var response = await _client.GetAsync("/api/v1/posts?tag=MisleadingOrFalse&untagged=true");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_pageSize_over_100_is_clamped()
    {
        var result = await _client.GetFromJsonAsync<PagedResult<PostSummaryDto>>("/api/v1/posts?pageSize=500", TestHttpExtensions.JsonOptions);

        Assert.Equal(100, result!.PageSize);
    }

    [Fact]
    public async Task List_sort_by_likeCount_desc_is_non_increasing_and_tie_broken_by_createdAt_desc()
    {
        var result = await _client.GetFromJsonAsync<PagedResult<PostSummaryDto>>("/api/v1/posts?sortBy=likeCount&sortDirection=desc&pageSize=100", TestHttpExtensions.JsonOptions);

        for (var i = 1; i < result!.Items.Count; i++)
        {
            var prev = result.Items[i - 1];
            var curr = result.Items[i];
            Assert.True(prev.LikeCount >= curr.LikeCount);
            if (prev.LikeCount == curr.LikeCount)
            {
                Assert.True(prev.CreatedAt >= curr.CreatedAt);
            }
        }
    }

    [Fact]
    public async Task ViewerHasLiked_is_true_only_for_the_liking_user()
    {
        var (authorToken, _) = await _client.RegisterUniqueUserAsync("vhl_author");
        var (likerToken, _) = await _client.RegisterUniqueUserAsync("vhl_liker");
        var (otherToken, _) = await _client.RegisterUniqueUserAsync("vhl_other");

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts").WithBearerToken(authorToken);
        createRequest.Content = JsonContent.Create(new CreatePostRequest("Viewer has liked test", "body"));
        var createResponse = await _client.SendAsync(createRequest);
        var post = await createResponse.Content.ReadFromJsonAsync<PostDetailDto>(TestHttpExtensions.JsonOptions);

        var likeRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{post!.Id}/likes/me").WithBearerToken(likerToken);
        (await _client.SendAsync(likeRequest)).EnsureSuccessStatusCode();

        var asLiker = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post.Id}").WithBearerToken(likerToken);
        var likerView = await (await _client.SendAsync(asLiker)).Content.ReadFromJsonAsync<PostDetailDto>(TestHttpExtensions.JsonOptions);

        var asOther = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post.Id}").WithBearerToken(otherToken);
        var otherView = await (await _client.SendAsync(asOther)).Content.ReadFromJsonAsync<PostDetailDto>(TestHttpExtensions.JsonOptions);

        Assert.True(likerView!.ViewerHasLiked);
        Assert.False(otherView!.ViewerHasLiked);
    }
}
