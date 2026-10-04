// Services/CategoryService.cs
using TradeHub.Dtos.Categories;
using TradeHub.Dtos.Products;
using TradeHub.Models;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<CategoryDto>> GetAllAsync(Guid userId)
    {
        var categories = await _categoryRepository.GetAllByUserIdAsync(userId);
        return categories.Select(ToDto).ToList();
    }

    public async Task<CategoryDto> CreateAsync(Guid userId, SaveCategoryDto dto)
    {
        var name = CleanName(dto);

        if (await _categoryRepository.ExistsByNameAsync(userId, name))
            throw new ArgumentException("You already have a category with this name");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId
        };

        await _categoryRepository.AddAsync(category);
        return ToDto(category);
    }

    public async Task UpdateAsync(Guid userId, Guid categoryId, SaveCategoryDto dto)
    {
        var category = await GetOwnedAsync(userId, categoryId);
        var name = CleanName(dto);

        if (await _categoryRepository.ExistsByNameAsync(userId, name, categoryId))
            throw new ArgumentException("You already have a category with this name");

        category.Name = name;

        if (!await _categoryRepository.UpdateAsync(category))
            throw new KeyNotFoundException("Category not found");
    }

    public async Task DeleteAsync(Guid userId, Guid categoryId)
    {
        await GetOwnedAsync(userId, categoryId);

        if (!await _categoryRepository.DeleteAsync(categoryId))
            throw new KeyNotFoundException("Category not found");
    }

    // Loads the category and makes sure it belongs to the logged-in user.
    // Someone else's category gets the same 404 as a missing one.
    private async Task<Category> GetOwnedAsync(Guid userId, Guid categoryId)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId);

        if (category == null || category.UserId != userId)
            throw new KeyNotFoundException("Category not found");

        return category;
    }

    private static string CleanName(SaveCategoryDto dto)
    {
        var name = (dto.Name ?? string.Empty).Trim();

        if (name.Length == 0)
            throw new ArgumentException("Category name is required");

        return name;
    }

    private static CategoryDto ToDto(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Products = c.Products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price ?? string.Empty,
            Description = p.Description ?? string.Empty,
            Image = p.Image ?? string.Empty,
            Quantity = p.Quantity,
            Status = p.Status,
            Discount = p.Discount ?? 0
        }).ToList()
    };
}