using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Verity.Application.Abstractions;
using Verity.Application.Auth;
using Verity.Application.Comments;
using Verity.Application.Data;
using Verity.Application.Likes;
using Verity.Application.Posts;
using Verity.Application.Tags;
using Verity.Infrastructure.Auth;
using Verity.Infrastructure.Seed;

namespace Verity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VerityDbContext>(options =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("Verity"),
                    npgsql => npgsql.EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IVerityDbContext>(sp => sp.GetRequiredService<VerityDbContext>());
        services.AddSingleton<IClock, SystemClock>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => o.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, IdentityPasswordHasherAdapter>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<AuthService>();
        services.AddScoped<PostService>();
        services.AddScoped<CommentService>();
        services.AddScoped<LikeService>();
        services.AddScoped<TagService>();
        services.AddScoped<DbSeeder>();

        return services;
    }
}
