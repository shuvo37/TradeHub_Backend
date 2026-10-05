using TradeHub.Models;

namespace TradeHub.Repositories;

public interface ILikeRepository
{
    Task<bool> AddAsync(Like like);
    Task<bool> DeleteAsync(Guid postId, Guid userId);
    Task<int> CountByPostIdAsync(Guid postId);
    Task<Dictionary<Guid, int>> CountByPostIdsAsync(List<Guid> postIds);
    Task<HashSet<Guid>> GetLikedPostIdsAsync(Guid userId, List<Guid> postIds);
}