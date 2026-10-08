namespace TradeHub.Enums;

// What happened.
public enum NotificationType
{
    FriendRequestReceived,   // someone sent me a friend request
    FriendRequestAccepted,   // someone accepted the request I sent
    PostCommented,           // someone commented on my post
    OrderAccepted,           // the seller accepted my order (stored as an integer: new values go at the END)
    OrderRejected            // the seller rejected my order
}
