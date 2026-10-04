namespace TradeHub.Models;

public class Like
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }
}