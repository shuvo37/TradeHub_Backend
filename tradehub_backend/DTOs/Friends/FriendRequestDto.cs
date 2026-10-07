namespace TradeHub.Dtos.Friends;

// A pending request I received: the request id plus who sent it
public class FriendRequestDto
{
    public Guid Id { get; set; }          // the request (use it to accept or decline)
    public Guid UserId { get; set; }      // the person who sent it
    public string Name { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
