using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Verity.Domain;

namespace Verity.Api.ErrorHandling;

/// <summary>
/// Maps DomainException codes, and unique-constraint violations surfaced by
/// concurrent writes (Postgres SQLSTATE 23505), to RFC 9457 ProblemDetails.
/// Anything else falls through as a 500 with a correlation id and no detail
/// outside Development.
/// </summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetailsService, IHostEnvironment environment, ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, type, title, detail) = Map(exception);

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Request to {Path} failed with {Status}", httpContext.Request.Path, status);
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Type = type,
                Title = title,
                Detail = environment.IsDevelopment() || status < 500 ? detail : null,
                Instance = httpContext.Request.Path,
                Extensions =
                {
                    ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier,
                },
            },
        });
    }

    private static (int Status, string Type, string Title, string? Detail) Map(Exception exception) => exception switch
    {
        DomainException { Code: DomainErrorCode.SelfLikeNotAllowed } e => (403, ProblemTypes.SelfLike, "You cannot like your own post.", e.Message),
        DomainException { Code: DomainErrorCode.InvalidCredentials } e => (401, ProblemTypes.InvalidCredentials, "Username or password is incorrect.", e.Message),
        DomainException { Code: DomainErrorCode.UsernameTaken } e => (409, ProblemTypes.UsernameTaken, "Username is already taken.", e.Message),
        DomainException { Code: DomainErrorCode.EmailTaken } e => (409, ProblemTypes.EmailTaken, "Email is already taken.", e.Message),
        DomainException { Code: DomainErrorCode.AlreadyLiked } e => (409, ProblemTypes.AlreadyLiked, "You have already liked this post.", e.Message),
        DomainException { Code: DomainErrorCode.AlreadyTagged } e => (409, ProblemTypes.AlreadyTagged, "This post already carries that tag.", e.Message),
        DomainException e => (400, ProblemTypes.Validation, "One or more fields are invalid.", e.Message),
        DbUpdateException { InnerException: PostgresException { SqlState: "23505" } pg } => MapUniqueViolation(pg),
        _ => (500, ProblemTypes.Internal, "An unexpected error occurred.", exception.Message),
    };

    private static (int, string, string, string?) MapUniqueViolation(PostgresException pg) => pg.TableName switch
    {
        "likes" => (409, ProblemTypes.AlreadyLiked, "You have already liked this post.", null),
        "post_tags" => (409, ProblemTypes.AlreadyTagged, "This post already carries that tag.", null),
        "users" => (409, ProblemTypes.UsernameTaken, "Username or email is already taken.", null),
        _ => (409, ProblemTypes.Internal, "The request conflicts with existing data.", null),
    };
}
