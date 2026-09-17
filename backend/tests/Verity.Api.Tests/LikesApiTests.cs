using System.Net;
using System.Net.Http.Json;
using Verity.Api.Tests.Infrastructure;
using Verity.Application.Likes;
using Verity.Application.Posts;

namespace Verity.Api.Tests;

[Collection("Api")]
public sealed class LikesApiTests(VerityApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(Guid PostId, string AuthorToken)> CreatePostAsync(string authorPrefix)
    {
        var (token, _) = await _client.RegisterUniqueUserAsync(authorPrefix);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts").WithBearerToken(token);
        request.Content = JsonContent.Create(new CreatePostRequest("A post for likes", "body"));
        var response = await _client.SendAsync(request);
        var post = await response.Content.ReadFromJsonAsync<PostDetailDto>(TestHttpExtensions.JsonOptions);
        return (post!.Id, token);
    }

    [Fact]
    public async Task Like_returns_201_with_count_plus_one()
    {
        var (postId, _) = await CreatePostAsync("likeauthor1");
        var (likerToken, _) = await _client.RegisterUniqueUserAsync("liker1");

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{postId}/likes/me").WithBearerToken(likerToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LikeStatusDto>(TestHttpExtensions.JsonOptions);
        Assert.Equal(1, body!.LikeCount);
        Assert.True(body.ViewerHasLiked);
    }

    [Fact]
    public async Task Second_like_returns_409_and_count_unchanged()
    {
        var (postId, _) = await CreatePostAsync("likeauthor2");
        var (likerToken, _) = await _client.RegisterUniqueUserAsync("liker2");

        var first = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{postId}/likes/me").WithBearerToken(likerToken);
        (await _client.SendAsync(first)).EnsureSuccessStatusCode();

        var second = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{postId}/likes/me").WithBearerToken(likerToken);
        var secondResponse = await _client.SendAsync(second);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var post = await _client.GetFromJsonAsync<PostDetailDto>($"/api/v1/posts/{postId}", TestHttpExtensions.JsonOptions);
        Assert.Equal(1, post!.LikeCount);
    }

    [Fact]
    public async Task Self_like_returns_403_and_count_unchanged()
    {
        var (postId, authorToken) = await CreatePostAsync("likeauthor3");

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{postId}/likes/me").WithBearerToken(authorToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var post = await _client.GetFromJsonAsync<PostDetailDto>($"/api/v1/posts/{postId}", TestHttpExtensions.JsonOptions);
        Assert.Equal(0, post!.LikeCount);
    }

    [Fact]
    public async Task Unlike_returns_200_with_count_minus_one()
    {
        var (postId, _) = await CreatePostAsync("likeauthor4");
        var (likerToken, _) = await _client.RegisterUniqueUserAsync("liker4");

        var like = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{postId}/likes/me").WithBearerToken(likerToken);
        (await _client.SendAsync(like)).EnsureSuccessStatusCode();

        var unlike = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/posts/{postId}/likes/me").WithBearerToken(likerToken);
        var response = await _client.SendAsync(unlike);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LikeStatusDto>(TestHttpExtensions.JsonOptions);
        Assert.Equal(0, body!.LikeCount);
    }

    [Fact]
    public async Task Unlike_when_not_liked_returns_404()
    {
        var (postId, _) = await CreatePostAsync("likeauthor5");
        var (likerToken, _) = await _client.RegisterUniqueUserAsync("liker5");

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/posts/{postId}/likes/me").WithBearerToken(likerToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Concurrent_double_like_yields_exactly_one_row_and_count_one()
    {
        var (postId, _) = await CreatePostAsync("likeauthor6");
        var (likerToken, _) = await _client.RegisterUniqueUserAsync("liker6");

        var attempts = Enumerable.Range(0, 8).Select(async _ =>
        {
            using var client = factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{postId}/likes/me").WithBearerToken(likerToken);
            return await client.SendAsync(request);
        });

        var responses = await Task.WhenAll(attempts);

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var post = await _client.GetFromJsonAsync<PostDetailDto>($"/api/v1/posts/{postId}", TestHttpExtensions.JsonOptions);
        Assert.Equal(1, post!.LikeCount);
    }
}
