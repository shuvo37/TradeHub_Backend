using TradeHub.Enums;
using TradeHub.Models;

namespace TradeHub.Repositories;

public interface INotificationRepository
{
    Task<Notification> AddAsync(Notification notification);
    // Newest first, with the actor loaded
    Task<List<Notification>> GetForUserAsync(Guid userId, int take);
    Task<int> CountUnreadAsync(Guid userId);
    // Marks every unread notification of this user as read, in one query
    Task MarkAllReadAsync(Guid userId);
    // Removes the notification(s) of one type that belong to a friend request
    Task DeleteByFriendshipAsync(Guid friendshipId, NotificationType type);
}
