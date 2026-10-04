using TradeHub.Dtos.Products;

namespace TradeHub.Services;

public interface IProductService
{
    Task<ProductDto> GetAsync(Guid productId);
    Task<List<ProductDto>> GetByCategoryAsync(Guid categoryId);
    Task<ProductDto> CreateAsync(Guid userId, Guid categoryId, SaveProductDto dto);
    Task UpdateAsync(Guid userId, Guid productId, SaveProductDto dto);
    Task DeleteAsync(Guid userId, Guid productId);
}