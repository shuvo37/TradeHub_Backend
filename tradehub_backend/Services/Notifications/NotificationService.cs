using TradeHub.Dtos.Notifications;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class NotificationService : INotificationService
{
    private const int ListLimit = 30;

    private readonly INotificationRepository _notifications;

    public NotificationService(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<List<NotificationDto>> GetAllAsync(Guid userId)
    {
        var items = await _notifications.GetForUserAsync(userId, ListLimit);

        return items.Select(n => new NotificationDto
        {
            Id = n.Id,
            Type = n.Type,
            ActorId = n.ActorId,
            ActorName = n.Actor!.Name,
            ActorUniqueName = n.Actor.UniqueName,
            ActorAvatar = n.Actor.Avatar ?? string.Empty,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        }).ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _notifications.CountUnreadAsync(userId);
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        await _notifications.MarkAllReadAsync(userId);
    }
}
