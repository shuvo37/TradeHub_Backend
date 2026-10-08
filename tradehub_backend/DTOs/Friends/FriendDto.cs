namespace TradeHub.Dtos.Friends;

// One person in my friends list
public class FriendDto
{
    public Guid UserId { get; set; }      // the friend (use it to open the profile or to unfriend)
    public string Name { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
