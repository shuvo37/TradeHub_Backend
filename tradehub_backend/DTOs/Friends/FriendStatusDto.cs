using TradeHub.Enums;

namespace TradeHub.Dtos.Friends;

// My relationship to one other user. RequestId is the friendship row (needed to accept or decline it);
// it is null when Status is None.
public class FriendStatusDto
{
    public FriendStatus Status { get; set; }
    public Guid? RequestId { get; set; }
}
