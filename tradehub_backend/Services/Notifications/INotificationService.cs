using TradeHub.Dtos.Notifications;

namespace TradeHub.Services;

public interface INotificationService
{
    // My latest notifications, newest first
    Task<List<NotificationDto>> GetAllAsync(Guid userId);
    // How many I have not read yet (the red number on the bell)
    Task<int> GetUnreadCountAsync(Guid userId);
    // Called when I open the bell: everything becomes read
    Task MarkAllReadAsync(Guid userId);
}
