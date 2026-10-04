// Dtos/Products/SaveProductDto.cs
using TradeHub.Enums;

namespace TradeHub.Dtos.Products;

public class SaveProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Price { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public int? Quantity { get; set; }
    public StockStatus? Status { get; set; }
    public decimal? Discount { get; set; }
}