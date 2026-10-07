namespace TradeHub.Enums;

// What the logged-in user sees about ANOTHER user (it depends on who sent the request)
public enum FriendStatus
{
    None,             // no request either way: show "Add friend"
    RequestSent,      // I sent a request and it is waiting
    RequestReceived,  // they sent me a request: show "Accept" / "Decline"
    Friends
}
