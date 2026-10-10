using TradeHub.Models;

namespace TradeHub.Repositories;

public interface ISuggestionRepository
{
    // Ids that must never be suggested: me, my friends, and everyone I have a pending request with (either direction)
    Task<List<Guid>> GetExcludedIdsAsync(Guid userId);
    // Counts for every possible suggestion: friends of my friends, people I traded or interacted with,
    // and the top sellers by recent order requests. 'since' limits comments and order requests to the recent period.
    Task<List<SuggestionSignals>> GetSignalsAsync(Guid userId, List<Guid> excluded, DateTimeOffset since, int topSellers);
    // Sellers with the most products (used only to fill the list when too few people have a score)
    Task<List<Guid>> GetActiveSellerIdsAsync(List<Guid> excluded, int take);
    // The users for these ids, in one query
    Task<List<User>> GetUsersAsync(List<Guid> ids);
}
