using System.Net.Http.Json;
using Verity.Api.Tests.Infrastructure;
using Verity.Application.Common;
using Verity.Application.Posts;

namespace Verity.Api.Tests;

[Collection("Api")]
public sealed class EfficiencyApiTests(VerityApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_posts_executes_at_most_two_sql_commands()
    {
        factory.CommandCounter.Reset();

        var response = await _client.GetAsync("/api/v1/posts?pageSize=20");
        response.EnsureSuccessStatusCode();

        Assert.True(factory.CommandCounter.Count <= 2, $"Expected <=2 SQL commands, saw {factory.CommandCounter.Count}.");
    }

    [Fact]
    public async Task Get_post_by_id_executes_exactly_one_sql_command()
    {
        var listResponse = await _client.GetAsync("/api/v1/posts?pageSize=1");
        var body = await listResponse.Content.ReadFromJsonAsync<PagedResult<PostSummaryDto>>(TestHttpExtensions.JsonOptions);
        var postId = body!.Items[0].Id;

        factory.CommandCounter.Reset();

        var response = await _client.GetAsync($"/api/v1/posts/{postId}");
        response.EnsureSuccessStatusCode();

        Assert.Equal(1, factory.CommandCounter.Count);
    }
}
