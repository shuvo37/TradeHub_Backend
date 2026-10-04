using TradeHub.Models;

namespace TradeHub.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<List<Product>> GetByCategoryIdAsync(Guid categoryId);
    Task<bool> ExistsByNameAsync(Guid categoryId, string name, Guid? excludeId = null);
    Task<Product> AddAsync(Product product);
    Task<bool> UpdateAsync(Product product);
    Task<bool> DeleteAsync(Guid id);
}