using TradeHub.Enums;

namespace TradeHub.Models;

// One row for a pair of users. Requester sent the request, Addressee received it.
// While Pending it is a request; once Accepted the two are friends (one row covers both people).
public class Friendship
{
    public Guid Id { get; set; }

    public Guid RequesterId { get; set; }
    public User? Requester { get; set; }

    public Guid AddresseeId { get; set; }
    public User? Addressee { get; set; }

    public FriendshipStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Has the addressee opened the Friends page since this request arrived?
    // Only the red number on the Friends icon uses it: the request itself stays until answered.
    public bool SeenByAddressee { get; set; }

    // The same row reads differently for each of the two people
    public FriendStatus StatusFor(Guid viewerId)
    {
        if (Status == FriendshipStatus.Accepted) return FriendStatus.Friends;
        return RequesterId == viewerId ? FriendStatus.RequestSent : FriendStatus.RequestReceived;
    }
}
