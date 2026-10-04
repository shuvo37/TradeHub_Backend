// Models/Category.cs
namespace TradeHub.Models;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public Guid UserId{ get; set; }
    public User? User { get; set; }
    public List<Product> Products { get; set; } = new();
}