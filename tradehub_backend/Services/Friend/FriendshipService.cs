using TradeHub.Dtos.Friends;
using TradeHub.Enums;
using TradeHub.Models;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class FriendshipService : IFriendshipService
{
    private readonly IFriendshipRepository _friendships;
    private readonly IUserRepository _users;
    private readonly INotificationRepository _notifications;

    private const int FriendsPageSize = 10;

    public FriendshipService(
        IFriendshipRepository friendships,
        IUserRepository users,
        INotificationRepository notifications)
    {
        _friendships = friendships;
        _users = users;
        _notifications = notifications;
    }

    public async Task<FriendStatusDto> GetStatusAsync(Guid userId, Guid otherUserId)
    {
        if (userId == otherUserId) return ToStatusDto(null, userId);

        var friendship = await _friendships.GetBetweenAsync(userId, otherUserId);
        return ToStatusDto(friendship, userId);
    }

    public async Task<FriendStatusDto> SendRequestAsync(Guid userId, Guid otherUserId)
    {
        if (userId == otherUserId)
            throw new ArgumentException("You can't send a friend request to yourself.");

        if (await _users.GetByIdAsync(otherUserId) == null)
            throw new KeyNotFoundException("User not found.");

        // One row per pair, whoever sent it: a second request in either direction is refused
        var existing = await _friendships.GetBetweenAsync(userId, otherUserId);
        if (existing != null)
        {
            throw new ArgumentException(existing.StatusFor(userId) switch
            {
                FriendStatus.Friends => "You are already friends.",
                FriendStatus.RequestSent => "You already sent a request to this person.",
                _ => "This person already sent you a request. Accept it instead."
            });
        }

        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            RequesterId = userId,
            AddresseeId = otherUserId,
            Status = FriendshipStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _friendships.AddAsync(friendship);

        // Tell the other person (the bell)
        await _notifications.AddAsync(NewNotification(
            recipientId: otherUserId,
            actorId: userId,
            NotificationType.FriendRequestReceived,
            friendship.Id));

        return ToStatusDto(friendship, userId);
    }

    public async Task<FriendsPageDto> GetFriendsAsync(Guid userId, DateTimeOffset? before)
    {
        // Ask for one extra row: if it comes back, there is a next page
        var rows = await _friendships.GetAcceptedAsync(userId, before, FriendsPageSize + 1);
        var page = rows.Take(FriendsPageSize).ToList();

        DateTimeOffset? nextCursor = null;
        if (page.Count > 0)
            nextCursor = page[^1].CreatedAt;

        return new FriendsPageDto
        {
            Items = page.Select(f =>
            {
                // One row covers both people: the friend is the one who is not me
                var friend = f.RequesterId == userId ? f.Addressee! : f.Requester!;
                return new FriendDto
                {
                    UserId = friend.Id,
                    Name = friend.Name,
                    UniqueName = friend.UniqueName,
                    Avatar = friend.Avatar ?? string.Empty,
                    Location = friend.Location ?? string.Empty
                };
            }).ToList(),
            HasMore = rows.Count > FriendsPageSize,
            NextCursor = nextCursor
        };
    }

    public async Task UnfriendAsync(Guid userId, Guid otherUserId)
    {
        // No row, a pending request, or a stranger all get the same 404
        var friendship = await _friendships.GetBetweenAsync(userId, otherUserId);
        if (friendship == null || friendship.Status != FriendshipStatus.Accepted)
            throw new KeyNotFoundException("Friend not found.");

        if (!await _friendships.DeleteAsync(friendship.Id))
            throw new KeyNotFoundException("Friend not found.");
    }

    public async Task<int> GetUnseenCountAsync(Guid userId)
    {
        return await _friendships.CountUnseenReceivedAsync(userId);
    }

    public async Task MarkSeenAsync(Guid userId)
    {
        await _friendships.MarkReceivedSeenAsync(userId);
    }

    public async Task<List<FriendRequestDto>> GetReceivedAsync(Guid userId)
    {
        var received = await _friendships.GetReceivedPendingAsync(userId);

        return received.Select(f => new FriendRequestDto
        {
            Id = f.Id,
            UserId = f.RequesterId,
            Name = f.Requester!.Name,
            UniqueName = f.Requester.UniqueName,
            Avatar = f.Requester.Avatar ?? string.Empty,
            Location = f.Requester.Location ?? string.Empty,
            CreatedAt = f.CreatedAt
        }).ToList();
    }

    public async Task AcceptAsync(Guid userId, Guid requestId)
    {
        // Someone else's request gets the same 404 as a missing one
        var friendship = await _friendships.GetByIdAsync(requestId);
        if (friendship == null || friendship.AddresseeId != userId)
            throw new KeyNotFoundException("Request not found.");

        if (friendship.Status != FriendshipStatus.Pending)
            throw new ArgumentException("This request was already accepted.");

        friendship.Status = FriendshipStatus.Accepted;

        if (!await _friendships.UpdateAsync(friendship))
            throw new KeyNotFoundException("Request not found.");

        // The request is answered, so "sent you a friend request" is no longer true for me
        await _notifications.DeleteByFriendshipAsync(requestId, NotificationType.FriendRequestReceived);

        // Tell the person who sent it (the bell)
        await _notifications.AddAsync(NewNotification(
            recipientId: friendship.RequesterId,
            actorId: userId,
            NotificationType.FriendRequestAccepted,
            friendship.Id));
    }

    public async Task DeleteRequestAsync(Guid userId, Guid requestId)
    {
        var friendship = await _friendships.GetByIdAsync(requestId);
        if (friendship == null || (friendship.AddresseeId != userId && friendship.RequesterId != userId))
            throw new KeyNotFoundException("Request not found.");

        if (friendship.Status != FriendshipStatus.Pending)
            throw new ArgumentException("You are already friends, so this is no longer a request.");

        if (!await _friendships.DeleteAsync(requestId))
            throw new KeyNotFoundException("Request not found.");

        // Declined or cancelled: the other person's "sent you a friend request" line must disappear too
        await _notifications.DeleteByFriendshipAsync(requestId, NotificationType.FriendRequestReceived);
    }

    private static Notification NewNotification(
        Guid recipientId, Guid actorId, NotificationType type, Guid friendshipId) => new()
    {
        Id = Guid.NewGuid(),
        RecipientId = recipientId,
        ActorId = actorId,
        Type = type,
        FriendshipId = friendshipId,
        IsRead = false,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static FriendStatusDto ToStatusDto(Friendship? friendship, Guid userId) => new()
    {
        Status = friendship == null ? FriendStatus.None : friendship.StatusFor(userId),
        RequestId = friendship?.Id
    };
}
