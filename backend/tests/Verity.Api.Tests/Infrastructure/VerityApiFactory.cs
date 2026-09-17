using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Verity.Infrastructure;
using Verity.Infrastructure.Seed;

namespace Verity.Api.Tests.Infrastructure;

/// <summary>
/// One Postgres container per test collection: started, migrated and seeded
/// exactly once (IAsyncLifetime.InitializeAsync), shared by every test class
/// in the "Api" collection. Each test still gets its own DbContext scope.
/// </summary>
public sealed class VerityApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Fixed password for this ephemeral, per-run Testcontainers database only - not a real credential.</summary>
    public const string TestSeedPassword = "TestOnly_Seed1!";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("verity_test")
        .WithUsername("verity_test")
        .WithPassword("verity_test")
        .Build();

    public CommandCountingInterceptor CommandCounter { get; } = new();

    /// <summary>
    /// TestServer gives every request the same simulated connection, so
    /// functional tests sharing one factory would otherwise all pile into
    /// one rate-limit partition. Defaults relaxed for the shared collection
    /// fixture (which xUnit activates with no constructor args); the
    /// dedicated rate-limit test sets this to null before first use to keep
    /// the real (appsettings) limit instead. Must be set before the host is
    /// built (i.e. before CreateClient()/Services is first touched).
    /// </summary>
    public int? AuthRateLimitPermitOverride { get; set; } = 100_000;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Force the host to build now (rather than lazily on the first
        // request) so migrations and seeding happen before any test runs.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VerityDbContext>();
        await db.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<VerityDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<VerityDbContext>(options =>
                options
                    .UseNpgsql(_container.GetConnectionString(), npgsql => npgsql.EnableRetryOnFailure())
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(CommandCounter));
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "verity-api-tests",
                ["Jwt:Audience"] = "verity-clients-tests",
                ["Jwt:SigningKey"] = "test-only-signing-key-at-least-32-characters-long",
                ["Seed:Enabled"] = "false", // InitializeAsync seeds explicitly, once
                ["Seed:Password"] = TestSeedPassword,
                ["Cors:AllowedOrigins:0"] = "http://localhost:4200",
            };

            if (AuthRateLimitPermitOverride is { } permitOverride)
            {
                settings["RateLimiting:Auth:PermitLimit"] = permitOverride.ToString();
            }

            config.AddInMemoryCollection(settings);
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.StopAsync();
        await base.DisposeAsync();
    }
}
