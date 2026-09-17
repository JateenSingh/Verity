namespace Verity.Application.Abstractions;

public enum PasswordVerificationOutcome
{
    Failed,
    Success,
    SuccessRehashNeeded,
}

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerificationOutcome Verify(string hashedPassword, string providedPassword);
}
