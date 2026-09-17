using System.Net;
using System.Net.Http.Json;
using Verity.Api.Tests.Infrastructure;
using Verity.Application.Posts;
using Verity.Application.Tags;

namespace Verity.Api.Tests;

[Collection("Api")]
public sealed class TagsApiTests(VerityApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();
    private const string TagRoute = "MisleadingOrFalse";

    private async Task<Guid> CreatePostAsync(string authorPrefix)
    {
        var (token, _) = await _client.RegisterUniqueUserAsync(authorPrefix);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts").WithBearerToken(token);
        request.Content = JsonContent.Create(new CreatePostRequest("A post for tagging", "body"));
        var response = await _client.SendAsync(request);
        var post = await response.Content.ReadFromJsonAsync<PostDetailDto>(TestHttpExtensions.JsonOptions);
        return post!.Id;
    }

    [Fact]
    public async Task Tag_as_regular_user_returns_403()
    {
        var postId = await CreatePostAsync("tagregular1");
        var (regularToken, _) = await _client.RegisterUniqueUserAsync("tagregular2");

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{postId}/tags/{TagRoute}").WithBearerToken(regularToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Tag_anonymous_returns_401()
    {
        var postId = await CreatePostAsync("taganon");

        var response = await _client.PutAsync($"/api/v1/posts/{postId}/tags/{TagRoute}", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Tag_as_moderator_returns_201_and_is_visible_on_the_post_with_reason_and_taggedBy()
    {
        var postId = await CreatePostAsync("tagmod1");
        var modToken = await _client.LoginAsync("mod_alice", VerityApiFactory.TestSeedPassword);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{postId}/tags/{TagRoute}").WithBearerToken(modToken);
        request.Content = JsonContent.Create(new TagRequest("Contradicts the published SDK behaviour."));
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var post = await _client.GetFromJsonAsync<PostDetailDto>($"/api/v1/posts/{postId}", TestHttpExtensions.JsonOptions);
        var tag = Assert.Single(post!.Tags);
        Assert.Equal("MisleadingOrFalse", tag.Tag);
        Assert.Equal("mod_alice", tag.TaggedBy.Username);
        Assert.Equal("Contradicts the published SDK behaviour.", tag.Reason);
    }

    [Fact]
    public async Task Tag_again_returns_409()
    {
        var postId = await CreatePostAsync("tagmod2");
        var modToken = await _client.LoginAsync("mod_alice", VerityApiFactory.TestSeedPassword);

        var first = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{postId}/tags/{TagRoute}").WithBearerToken(modToken);
        (await _client.SendAsync(first)).EnsureSuccessStatusCode();

        var second = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{postId}/tags/{TagRoute}").WithBearerToken(modToken);
        var secondResponse = await _client.SendAsync(second);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Remove_tag_returns_204_and_remove_again_returns_404()
    {
        var postId = await CreatePostAsync("tagmod3");
        var modToken = await _client.LoginAsync("mod_bob", VerityApiFactory.TestSeedPassword);

        var tag = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{postId}/tags/{TagRoute}").WithBearerToken(modToken);
        (await _client.SendAsync(tag)).EnsureSuccessStatusCode();

        var remove = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/posts/{postId}/tags/{TagRoute}").WithBearerToken(modToken);
        var removeResponse = await _client.SendAsync(remove);
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var removeAgain = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/posts/{postId}/tags/{TagRoute}").WithBearerToken(modToken);
        var removeAgainResponse = await _client.SendAsync(removeAgain);
        Assert.Equal(HttpStatusCode.NotFound, removeAgainResponse.StatusCode);
    }
}
