using TradeHub.Enums;

namespace TradeHub.Models;

// One line in somebody's bell list: "Actor did something". Recipient is the person who sees it.
public class Notification
{
    public Guid Id { get; set; }

    public Guid RecipientId { get; set; }

    // The person who caused it (the one who sent or accepted the request)
    public Guid ActorId { get; set; }
    public User? Actor { get; set; }

    public NotificationType Type { get; set; }

    // The friend request this is about. Used to remove the "sent you a request" line
    // when the request is answered or cancelled. Not a foreign key on purpose.
    public Guid? FriendshipId { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
