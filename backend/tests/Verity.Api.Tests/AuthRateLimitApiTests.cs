using System.Net;
using System.Net.Http.Json;
using Verity.Api.Tests.Infrastructure;
using Verity.Application.Auth;

namespace Verity.Api.Tests;

/// <summary>
/// Deliberately NOT in the shared "Api" collection: this needs the real
/// (unrelaxed) rate-limit configuration and its own connection partition,
/// so it gets its own factory and container rather than sharing one where
/// every other test's auth calls would pollute the same bucket.
/// </summary>
public sealed class AuthRateLimitApiTests : IAsyncLifetime
{
    private readonly VerityApiFactory _factory = new() { AuthRateLimitPermitOverride = null };

    public Task InitializeAsync() => ((IAsyncLifetime)_factory).InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_factory).DisposeAsync();

    [Fact]
    public async Task Login_rate_limit_returns_429_after_ten_attempts_per_ip()
    {
        using var client = _factory.CreateClient();
        var login = new LoginRequest($"rate-limit-probe-{Guid.NewGuid():N}", "WhateverPassword1");

        HttpResponseMessage? last = null;
        for (var i = 0; i < 11; i++)
        {
            last = await client.PostAsJsonAsync("/api/v1/auth/login", login);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        Assert.True(last.Headers.RetryAfter is not null);
    }
}
