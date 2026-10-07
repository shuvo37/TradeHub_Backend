using TradeHub.Dtos.Friends;

namespace TradeHub.Services;

public interface IFriendshipService
{
    // How I am connected to another user (None / RequestSent / RequestReceived / Friends)
    Task<FriendStatusDto> GetStatusAsync(Guid userId, Guid otherUserId);
    // Send a friend request to another user
    Task<FriendStatusDto> SendRequestAsync(Guid userId, Guid otherUserId);
    // Pending requests other people sent to me
    Task<List<FriendRequestDto>> GetReceivedAsync(Guid userId);
    // Requests I have not looked at yet (the red number on the Friends icon)
    Task<int> GetUnseenCountAsync(Guid userId);
    // I opened the Friends page: all my waiting requests count as seen
    Task MarkSeenAsync(Guid userId);
    // Only the person who received the request can accept it
    Task AcceptAsync(Guid userId, Guid requestId);
    // The receiver declines it, or the sender cancels it (only while it is still pending)
    Task DeleteRequestAsync(Guid userId, Guid requestId);
}
