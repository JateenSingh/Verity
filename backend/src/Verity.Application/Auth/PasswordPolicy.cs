namespace Verity.Application.Auth;

public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static bool IsSatisfiedBy(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        if (password.Length < MinLength || password.Length > MaxLength)
        {
            return false;
        }

        return password.Any(char.IsLetter) && password.Any(char.IsDigit);
    }
}
