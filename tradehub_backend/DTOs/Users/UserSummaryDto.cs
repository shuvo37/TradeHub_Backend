using TradeHub.Enums;

namespace TradeHub.Dtos.Users;

// One row of the search results: just enough to recognise the person and open their profile.
// (The profile page itself loads the full UserDto.)
// FriendStatus / RequestId say how I am connected to this person, so the row can show the right button.
public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public FriendStatus FriendStatus { get; set; } = FriendStatus.None;
    public Guid? RequestId { get; set; }
}
