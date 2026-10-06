// Dtos/Comments/CommentPageDto.cs
namespace TradeHub.Dtos.Comments;

// One page of a post's comments (oldest first).
// HasMore tells the client whether a "View more comments" button is needed.
public class CommentPageDto
{
    public List<CommentDto> Items { get; set; } = new();
    public bool HasMore { get; set; }
}
