using TradeHub.Enums;

namespace TradeHub.Dtos.Notifications;

// One line of the bell list, with who did it so the page can show their photo and name
public class NotificationDto
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorUniqueName { get; set; } = string.Empty;
    public string ActorAvatar { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
