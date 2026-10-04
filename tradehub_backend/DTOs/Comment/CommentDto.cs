// Dtos/Comments/CommentDto.cs
namespace TradeHub.Dtos.Comments;

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorAvatar { get; set; } = string.Empty;
}