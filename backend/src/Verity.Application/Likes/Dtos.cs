namespace Verity.Application.Likes;

public sealed record LikeStatusDto(Guid PostId, int LikeCount, bool ViewerHasLiked);
