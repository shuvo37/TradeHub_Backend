// Dtos/Categories/CategoryDto.cs
using TradeHub.Dtos.Products;

namespace TradeHub.Dtos.Categories;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ProductDto> Products { get; set; } = new();
}