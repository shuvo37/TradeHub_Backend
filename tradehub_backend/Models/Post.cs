namespace TradeHub.Models;

public class Post
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Image { get; set; }

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }
    public List<Like> Likes { get; set; } = new();

    public List<Comment> Comments { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; }
}