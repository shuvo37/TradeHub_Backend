using TradeHub.Models;

namespace TradeHub.Repositories;

public interface IDiscoverRepository
{
    // Ids of my accepted friends (their posts are in my news feed, so Discover skips them)
    Task<List<Guid>> GetFriendIdsAsync(Guid userId);
    // Recent posts that are not mine and not my friends', newest first (a revived post counts from its revive time).
    // Author, product and the product's category are loaded.
    Task<List<Post>> GetCandidatePostsAsync(Guid userId, List<Guid> friendIds, DateTimeOffset since, int take);
    // For each author: how many of MY friends are also friends with that author
    Task<Dictionary<Guid, int>> GetMutualFriendCountsAsync(List<Guid> friendIds, List<Guid> authorIds);
    // Authors I have an order with, in either direction (rejected orders do not count)
    Task<HashSet<Guid>> GetTradePartnerIdsAsync(Guid userId, List<Guid> authorIds);
    // For each author: order requests received as a seller since 'since' (any status)
    Task<Dictionary<Guid, int>> GetSellerOrderCountsAsync(List<Guid> authorIds, DateTimeOffset since);
    // Category names I care about: the categories I ordered from, and the categories of products attached to posts I liked
    Task<HashSet<string>> GetInterestCategoriesAsync(Guid userId);
}
