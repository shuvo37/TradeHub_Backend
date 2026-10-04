using TradeHub.Models;

namespace TradeHub.Repositories;

public interface ILikeRepository
{
    Task<bool> AddAsync(Like like);
    Task<bool> DeleteAsync(Guid postId, Guid userId);
    Task<int> CountByPostIdAsync(Guid postId);
}