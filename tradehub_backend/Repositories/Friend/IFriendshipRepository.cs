using TradeHub.Models;

namespace TradeHub.Repositories;

public interface IFriendshipRepository
{
    Task<Friendship?> GetByIdAsync(Guid id);
    // The row between two users, whoever sent it (null = no relationship)
    Task<Friendship?> GetBetweenAsync(Guid userA, Guid userB);
    // My rows with any of these users, in ONE query (used by search)
    Task<List<Friendship>> GetBetweenManyAsync(Guid userId, List<Guid> otherIds);
    // Pending requests sent TO this user, newest first, with the sender loaded
    Task<List<Friendship>> GetReceivedPendingAsync(Guid userId);
    // How many pending requests sent to this user they have not seen yet (the Friends icon badge)
    Task<int> CountUnseenReceivedAsync(Guid userId);
    // Marks all of this user's pending received requests as seen, in one query
    Task MarkReceivedSeenAsync(Guid userId);
    Task<Friendship> AddAsync(Friendship friendship);
    Task<bool> UpdateAsync(Friendship friendship);
    Task<bool> DeleteAsync(Guid id);
}
