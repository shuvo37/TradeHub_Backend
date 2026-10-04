// Repositories/ICategoryRepository.cs
using TradeHub.Models;

namespace TradeHub.Repositories;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllByUserIdAsync(Guid userId);
    Task<Category?> GetByIdAsync(Guid id);
    Task<bool> ExistsByNameAsync(Guid userId, string name, Guid? excludeId = null);
    Task<Category> AddAsync(Category category);
    Task<bool> UpdateAsync(Category category);
    Task<bool> DeleteAsync(Guid id);
}