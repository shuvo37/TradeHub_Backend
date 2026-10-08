using TradeHub.Enums;

namespace TradeHub.Models;

// One line in somebody's bell list: "Actor did something". Recipient is the person who sees it.
public class Notification
{
    public Guid Id { get; set; }

    public Guid RecipientId { get; set; }

    // The person who caused it (the one who sent or accepted the request, or wrote the comment)
    public Guid ActorId { get; set; }
    public User? Actor { get; set; }

    public NotificationType Type { get; set; }

    // The friend request this is about. Used to remove the "sent you a request" line
    // when the request is answered or cancelled. Not a foreign key on purpose.
    public Guid? FriendshipId { get; set; }

    // The post that was commented on (opened in the popup when the line is clicked).
    // Not a foreign key on purpose, like FriendshipId.
    public Guid? PostId { get; set; }

    // The comment itself. Used to remove the line when that comment is deleted.
    // Not a foreign key on purpose.
    public Guid? CommentId { get; set; }

    // The order this is about (OrderAccepted / OrderRejected): opened as a summary card when the line is clicked.
    // Not a foreign key on purpose: the line stays in the bell even if the order row is ever removed.
    public Guid? OrderId { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
