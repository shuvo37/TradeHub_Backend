// Dtos/Posts/FeedPageDto.cs
namespace TradeHub.Dtos.Posts;

// One page of the news feed (newest first; a revived post counts from its revive time).
// HasMore tells the client whether the "More" button is needed.
public class FeedPageDto
{
    public List<PostDto> Items { get; set; } = new();
    public bool HasMore { get; set; }

    // Where the next page starts: the feed time (RevivedAt, or CreatedAt if never revived) of the last post in Items.
    // The client sends it back unchanged as ?before=... when "More" is pressed. null when the page is empty.
    public DateTimeOffset? NextCursor { get; set; }
}
