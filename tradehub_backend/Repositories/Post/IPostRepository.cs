using TradeHub.Models;

namespace TradeHub.Repositories;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(Guid id);
    Task<Post?> GetDetailsByIdAsync(Guid id);
    Task<List<Post>> GetDetailsByUserIdAsync(Guid userId);
    // One page of the news feed: my posts and my accepted friends' posts, newest first.
    // 'before' is the CreatedAt of the oldest post the client already has (null = start from the newest).
    Task<List<Post>> GetFeedAsync(Guid userId, DateTimeOffset? before, int take);
    Task<Post> AddAsync(Post post);
    Task<bool> UpdateAsync(Post post);
    Task<bool> DeleteAsync(Guid id);
}