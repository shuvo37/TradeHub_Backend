namespace TradeHub.Repositories;

// What the search needs to know about one matching category
public class CategorySearchRow
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string SellerAvatar { get; set; } = string.Empty;

    public int ProductCount { get; set; }
}
