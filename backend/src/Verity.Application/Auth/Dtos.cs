using System.ComponentModel.DataAnnotations;

namespace Verity.Application.Auth;

public sealed record RegisterRequest(
    [Required, RegularExpression("^[a-zA-Z0-9_]{3,32}$")] string Username,
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, PasswordPolicy] string Password);

public sealed record LoginRequest(
    [Required] string Username,
    [Required] string Password);

public sealed record UserSummaryDto(Guid Id, string Username, string Role);

public sealed record AuthResponseDto(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, UserSummaryDto User);
