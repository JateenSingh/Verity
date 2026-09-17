namespace Verity.Domain;

public sealed class DomainException : Exception
{
    public DomainErrorCode Code { get; }

    public DomainException(DomainErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }
}
