using Microsoft.EntityFrameworkCore;
using Verity.Application.Abstractions;
using Verity.Application.Data;
using Verity.Domain;
using Verity.Domain.Entities;
using Verity.Domain.Enums;

namespace Verity.Application.Auth;

public sealed class AuthService(
    IVerityDbContext db,
    IPasswordHasher hasher,
    ITokenService tokenService,
    IClock clock)
{
    // Verified against on every unknown-username login so an unknown user takes
    // roughly the same time as a wrong-password one; existence is not leaked via timing.
    private static readonly Lazy<string> DummyHash = new(() =>
        new Microsoft.AspNetCore.Identity.PasswordHasher<object>().HashPassword(new object(), "Correct-Horse-Battery-Staple-1"));

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var usernameLower = request.Username.Trim().ToLowerInvariant();
        var emailLower = request.Email.Trim().ToLowerInvariant();

        var conflict = await db.Users
            .Where(u => EF.Property<string>(u, "UsernameLower") == usernameLower
                     || EF.Property<string>(u, "EmailLower") == emailLower)
            .Select(u => new { UsernameLower = EF.Property<string>(u, "UsernameLower") })
            .FirstOrDefaultAsync(ct);

        if (conflict is not null)
        {
            var code = conflict.UsernameLower == usernameLower ? DomainErrorCode.UsernameTaken : DomainErrorCode.EmailTaken;
            var field = code == DomainErrorCode.UsernameTaken ? "Username" : "Email";
            throw new DomainException(code, $"{field} is already taken.");
        }

        var passwordHash = hasher.Hash(request.Password);
        var user = User.Create(request.Username, request.Email, passwordHash, UserRole.User, createdAt: clock.UtcNow);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var usernameLower = request.Username.Trim().ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => EF.Property<string>(u, "UsernameLower") == usernameLower, ct);

        var outcome = hasher.Verify(user?.PasswordHash ?? DummyHash.Value, request.Password);

        if (user is null || outcome == PasswordVerificationOutcome.Failed)
        {
            throw new DomainException(DomainErrorCode.InvalidCredentials, "Username or password is incorrect.");
        }

        if (outcome == PasswordVerificationOutcome.SuccessRehashNeeded)
        {
            user.UpdatePasswordHash(hasher.Hash(request.Password));
            await db.SaveChangesAsync(ct);
        }

        return BuildResponse(user);
    }

    private AuthResponseDto BuildResponse(User user)
    {
        var issued = tokenService.IssueToken(user);
        var summary = new UserSummaryDto(user.Id, user.Username, user.Role.ToString());
        return new AuthResponseDto(issued.AccessToken, "Bearer", issued.ExpiresAt, summary);
    }
}
