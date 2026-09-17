using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Verity.Api.Auth;
using Verity.Api.ErrorHandling;
using Verity.Application.Abstractions;
using Verity.Infrastructure;
using Scalar.AspNetCore;
using Verity.Infrastructure.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1.0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Verity.Application.Abstractions.ICurrentUser, CurrentUser>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Otherwise "sub"/"name" get remapped to long ClaimTypes URIs on the
        // inbound principal and CurrentUser's claim lookups silently miss.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"] ?? string.Empty)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role",
            NameClaimType = "name",
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ModeratorOnly", policy => policy.RequireClaim("role", nameof(Verity.Domain.Enums.UserRole.Moderator)));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            // Read at first-use time (not captured once before the host
            // finishes building) so configurable, not just hardcoded to 10 -
            // integration tests that only care about functional behaviour
            // raise this well above what a test run's request volume could
            // ever hit, while a dedicated rate-limit test uses its own
            // factory with the real default to verify the actual limit.
            PermitLimit = httpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("RateLimiting:Auth:PermitLimit", 10),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));

    options.OnRejected = (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }

        return ValueTask.CompletedTask;
    };
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .WithTitle("Verity API")
        .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch));
}

app.UseHttpsRedirection();

app.UseCors("WebClient");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", async (Verity.Infrastructure.VerityDbContext db, CancellationToken ct) =>
{
    var healthy = await db.Database.CanConnectAsync(ct);
    return healthy ? Results.Ok(new { status = "healthy" }) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Verity.Infrastructure.VerityDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Seed:Enabled"))
    {
        var seeder = scope.ServiceProvider.GetRequiredService<Verity.Infrastructure.Seed.DbSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
    }
}

app.Run();

public partial class Program;
