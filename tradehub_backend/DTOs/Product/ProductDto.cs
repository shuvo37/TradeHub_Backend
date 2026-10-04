// Dtos/Products/ProductDto.cs
using TradeHub.Enums;

namespace TradeHub.Dtos.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public int? Quantity { get; set; }
    public StockStatus? Status { get; set; }
    public decimal Discount { get; set; }
}