using System.Net;
using System.Net.Http.Json;
using Verity.Api.Tests.Infrastructure;
using Verity.Application.Auth;

namespace Verity.Api.Tests;

[Collection("Api")]
public sealed class AuthApiTests(VerityApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_returns_201_with_token()
    {
        var unique = Guid.NewGuid().ToString("N")[..10];
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest($"newuser_{unique}", $"newuser_{unique}@example.test", VerityApiFactory.TestSeedPassword));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(TestHttpExtensions.JsonOptions);
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));
    }

    [Fact]
    public async Task Register_duplicate_username_returns_409()
    {
        var (_, user) = await _client.RegisterUniqueUserAsync("dupuser");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(user.Username, "different@example.test", VerityApiFactory.TestSeedPassword));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_duplicate_email_is_case_insensitive_and_returns_409()
    {
        var unique = Guid.NewGuid().ToString("N")[..10];
        var email = $"casetest_{unique}@example.test";
        var first = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest($"caseuser1_{unique}", email, VerityApiFactory.TestSeedPassword));
        first.EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest($"caseuser2_{unique}", email.ToUpperInvariant(), VerityApiFactory.TestSeedPassword));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_weak_password_returns_400()
    {
        var unique = Guid.NewGuid().ToString("N")[..10];
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest($"weakpw_{unique}", $"weakpw_{unique}@example.test", "weak"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_wrong_password_returns_401()
    {
        var (_, user) = await _client.RegisterUniqueUserAsync("wrongpw");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Username, "TotallyWrong1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_unknown_user_returns_401_with_same_body_as_wrong_password()
    {
        var (_, user) = await _client.RegisterUniqueUserAsync("knownuser");

        var wrongPasswordResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Username, "TotallyWrong1"));
        var unknownUserResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest($"nosuchuser_{Guid.NewGuid():N}", "TotallyWrong1"));

        Assert.Equal(HttpStatusCode.Unauthorized, unknownUserResponse.StatusCode);
        var wrongPasswordBody = await wrongPasswordResponse.Content.ReadAsStringAsync();
        var unknownUserBody = await unknownUserResponse.Content.ReadAsStringAsync();
        Assert.Equal(
            System.Text.Json.JsonDocument.Parse(wrongPasswordBody).RootElement.GetProperty("type").GetString(),
            System.Text.Json.JsonDocument.Parse(unknownUserBody).RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task GetMe_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_with_token_returns_current_user()
    {
        var (token, user) = await _client.RegisterUniqueUserAsync("meuser");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me").WithBearerToken(token);
        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<UserSummaryDto>(TestHttpExtensions.JsonOptions);
        Assert.Equal(user.Id, body!.Id);
    }
}
