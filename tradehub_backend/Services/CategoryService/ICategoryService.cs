// Services/ICategoryService.cs
using TradeHub.Dtos.Categories;

namespace TradeHub.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(Guid userId);
    Task<CategoryDto> CreateAsync(Guid userId, SaveCategoryDto dto);
    Task UpdateAsync(Guid userId, Guid categoryId, SaveCategoryDto dto);
    Task DeleteAsync(Guid userId, Guid categoryId);
}
