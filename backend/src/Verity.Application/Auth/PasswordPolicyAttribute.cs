using System.ComponentModel.DataAnnotations;

namespace Verity.Application.Auth;

public sealed class PasswordPolicyAttribute : ValidationAttribute
{
    public PasswordPolicyAttribute()
    {
        ErrorMessage = $"Password must be {PasswordPolicy.MinLength}-{PasswordPolicy.MaxLength} characters and include at least one letter and one digit.";
    }

    public override bool IsValid(object? value) => value is string password && PasswordPolicy.IsSatisfiedBy(password);
}
