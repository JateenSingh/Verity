using System.Net;
using System.Net.Http.Json;
using Verity.Api.Tests.Infrastructure;
using Verity.Application.Comments;
using Verity.Application.Common;
using Verity.Application.Posts;

namespace Verity.Api.Tests;

[Collection("Api")]
public sealed class CommentsApiTests(VerityApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<Guid> CreatePostAsync(string authorPrefix)
    {
        var (token, _) = await _client.RegisterUniqueUserAsync(authorPrefix);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts").WithBearerToken(token);
        request.Content = JsonContent.Create(new CreatePostRequest("A post for comments", "body"));
        var response = await _client.SendAsync(request);
        var post = await response.Content.ReadFromJsonAsync<PostDetailDto>(TestHttpExtensions.JsonOptions);
        return post!.Id;
    }

    [Fact]
    public async Task Create_anonymous_returns_401()
    {
        var postId = await CreatePostAsync("commentanon");

        var response = await _client.PostAsJsonAsync($"/api/v1/posts/{postId}/comments", new CreateCommentRequest("hi"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_authenticated_returns_201()
    {
        var postId = await CreatePostAsync("commentauth1");
        var (token, _) = await _client.RegisterUniqueUserAsync("commentauth2");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/posts/{postId}/comments").WithBearerToken(token);
        request.Content = JsonContent.Create(new CreateCommentRequest("A useful comment"));
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task List_is_oldest_first_and_paged()
    {
        var postId = await CreatePostAsync("commentlist");
        var (token, _) = await _client.RegisterUniqueUserAsync("commentlister");

        for (var i = 0; i < 3; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/posts/{postId}/comments").WithBearerToken(token);
            request.Content = JsonContent.Create(new CreateCommentRequest($"Comment number {i}"));
            (await _client.SendAsync(request)).EnsureSuccessStatusCode();
        }

        var result = await _client.GetFromJsonAsync<PagedResult<CommentDto>>($"/api/v1/posts/{postId}/comments", TestHttpExtensions.JsonOptions);

        Assert.Equal(3, result!.TotalCount);
        var sorted = result.Items.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id).ToList();
        Assert.Equal(sorted.Select(c => c.Id), result.Items.Select(c => c.Id));
    }

    [Fact]
    public async Task List_for_unknown_post_returns_404()
    {
        var response = await _client.GetAsync($"/api/v1/posts/{Guid.NewGuid()}/comments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
