// Models/Product.cs
using TradeHub.Enums;

namespace TradeHub.Models;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Price { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public string? Image { get; set; } = string.Empty;

    public int? Quantity { get; set; }        // set when tracking exact count
    public StockStatus? Status { get; set; }  // set when only Available/Unavailable

    public decimal? Discount { get; set; }
    public Guid CategoryId { get; set; }
    public Category?  Category { get; set; }


}