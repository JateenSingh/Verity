using Microsoft.AspNetCore.Identity;
using Verity.Application.Abstractions;

namespace Verity.Infrastructure.Auth;

/// <summary>
/// Wraps ASP.NET Identity's PasswordHasher (PBKDF2-HMAC-SHA512) without pulling in
/// the rest of the Identity framework - no user store, no cookie scheme, no UI.
/// </summary>
public sealed class IdentityPasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<object> _inner = new();
    private static readonly object HasherSubject = new();

    public string Hash(string password) => _inner.HashPassword(HasherSubject, password);

    public PasswordVerificationOutcome Verify(string hashedPassword, string providedPassword) =>
        _inner.VerifyHashedPassword(HasherSubject, hashedPassword, providedPassword) switch
        {
            PasswordVerificationResult.Success => PasswordVerificationOutcome.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationOutcome.SuccessRehashNeeded,
            _ => PasswordVerificationOutcome.Failed,
        };
}
