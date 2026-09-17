using System.Net.Http.Json;
using System.Text.Json;
using Verity.Application.Auth;

namespace Verity.Api.Tests.Infrastructure;

public static class TestHttpExtensions
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<string> LoginAsync(this HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        return body!.AccessToken;
    }

    public static async Task<(string Token, UserSummaryDto User)> RegisterUniqueUserAsync(this HttpClient client, string prefix)
    {
        var unique = Guid.NewGuid().ToString("N")[..10];
        var username = $"{prefix}_{unique}";
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(username, $"{username}@example.test", "TestOnly_Register1!"));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        return (body!.AccessToken, body.User);
    }

    public static HttpRequestMessage WithBearerToken(this HttpRequestMessage request, string token)
    {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
