// Dtos/Posts/PostDto.cs
using TradeHub.Dtos.Products;

namespace TradeHub.Dtos.Posts;

public class PostDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorAvatar { get; set; } = string.Empty;

    public ProductDto? Product { get; set; }

    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool LikedByMe { get; set; } // did the logged-in user like this post
}