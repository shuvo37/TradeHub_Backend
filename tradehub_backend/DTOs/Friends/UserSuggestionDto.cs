using TradeHub.Dtos.Users;

namespace TradeHub.Dtos.Friends;

// One row of "People you may know".
// It is a UserSummaryDto (id, name, uniqueName, avatar, location, friendStatus, requestId), so the same
// FriendButton can be used. Suggested people are never friends and never have a pending request,
// so FriendStatus is always None and RequestId is always null.
public class UserSuggestionDto : UserSummaryDto
{
    // How many friends we have in common (0 when the person is suggested for another reason)
    public int MutualCount { get; set; }

    // The biggest reason in plain words, e.g. "3 mutual friends", "You ordered from them", "Popular seller"
    public string Reason { get; set; } = string.Empty;
}
