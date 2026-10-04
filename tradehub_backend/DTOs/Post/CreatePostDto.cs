// Dtos/Posts/CreatePostDto.cs
namespace TradeHub.Dtos.Posts;

public class CreatePostDto
{
    public string Text { get; set; } = string.Empty;
    public string? Image { get; set; }
    public Guid? ProductId { get; set; }
}