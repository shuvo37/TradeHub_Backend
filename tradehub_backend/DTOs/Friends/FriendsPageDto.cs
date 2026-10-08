namespace TradeHub.Dtos.Friends;

// One page of my friends (newest friendship first).
// HasMore tells the client whether the "More" button is needed.
public class FriendsPageDto
{
    public List<FriendDto> Items { get; set; } = new();
    public bool HasMore { get; set; }

    // Where the next page starts: the CreatedAt of the last friendship in Items.
    // The client sends it back unchanged as ?before=... when "More" is pressed. null when the page is empty.
    public DateTimeOffset? NextCursor { get; set; }
}
