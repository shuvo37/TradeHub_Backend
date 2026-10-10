using TradeHub.Enums;

namespace TradeHub.Repositories;

// What the search needs to know about one matching product (the repository only fetches; the service scores)
public class ProductSearchRow
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public int? Quantity { get; set; }
    public StockStatus? Status { get; set; }
    public decimal Discount { get; set; } // 0 when there is no discount

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string SellerAvatar { get; set; } = string.Empty;
}
