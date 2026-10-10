// Dtos/Search/CategorySearchItemDto.cs
namespace TradeHub.Dtos.Search;

// One category in the search results (a seller's category whose name matches the search)
public class CategorySearchItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string SellerAvatar { get; set; } = string.Empty; // "" when the seller has no avatar

    public int ProductCount { get; set; }
}
