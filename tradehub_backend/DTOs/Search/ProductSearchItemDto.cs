// Dtos/Search/ProductSearchItemDto.cs
using TradeHub.Dtos.Products;

namespace TradeHub.Dtos.Search;

// One product in the search results: the product itself plus the category it sits in and the seller who sells it
public class ProductSearchItemDto
{
    public ProductDto Product { get; set; } = new();

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string SellerAvatar { get; set; } = string.Empty; // "" when the seller has no avatar
}
