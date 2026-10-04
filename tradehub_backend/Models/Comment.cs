// Models/Comment.cs
namespace TradeHub.Models;

public class Comment
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }
}