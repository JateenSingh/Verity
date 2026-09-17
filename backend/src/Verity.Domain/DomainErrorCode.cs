namespace Verity.Domain;

public enum DomainErrorCode
{
    InvalidUsername,
    InvalidEmail,
    InvalidTitle,
    InvalidBody,
    InvalidComment,
    InvalidReason,
    SelfLikeNotAllowed,
    UsernameTaken,
    EmailTaken,
    InvalidCredentials,
    AlreadyLiked,
    AlreadyTagged,
}
