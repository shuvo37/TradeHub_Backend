namespace TradeHub.Services;

public interface ILikeService
{
    Task LikeAsync(Guid userId, Guid postId);
    Task UnlikeAsync(Guid userId, Guid postId);
    Task<int> CountAsync(Guid postId);
}