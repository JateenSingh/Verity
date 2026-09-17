namespace Verity.Api.ErrorHandling;

public static class ProblemTypes
{
    private const string BaseUri = "https://verity.local/problems/";

    public const string Validation = BaseUri + "validation";
    public const string Unauthorized = BaseUri + "unauthorized";
    public const string InvalidCredentials = BaseUri + "invalid-credentials";
    public const string Forbidden = BaseUri + "forbidden";
    public const string SelfLike = BaseUri + "self-like";
    public const string NotFound = BaseUri + "not-found";
    public const string AlreadyLiked = BaseUri + "already-liked";
    public const string AlreadyTagged = BaseUri + "already-tagged";
    public const string UsernameTaken = BaseUri + "username-taken";
    public const string EmailTaken = BaseUri + "email-taken";
    public const string RateLimited = BaseUri + "rate-limited";
    public const string Internal = BaseUri + "internal";
}
