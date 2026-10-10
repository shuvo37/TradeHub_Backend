// Dtos/Posts/DiscoverPageDto.cs
namespace TradeHub.Dtos.Posts;

// One page of Discover "For you" posts, best score first.
// The posts come from a ranked list of at most 50, so the page is found by counting (skip), not by a timestamp.
public class DiscoverPageDto
{
    public List<PostDto> Items { get; set; } = new();
    public bool HasMore { get; set; }

    // Where the next page starts: the client sends it back unchanged as ?skip=... when "More" is pressed
    public int NextSkip { get; set; }
}
