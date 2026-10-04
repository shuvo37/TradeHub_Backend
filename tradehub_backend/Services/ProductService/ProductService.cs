using TradeHub.Dtos.Products;
using TradeHub.Enums;
using TradeHub.Models;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    // Any logged-in user can read a product (visiting another user's store).
    public async Task<ProductDto> GetAsync(Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
            throw new KeyNotFoundException("Product not found");

        return ToDto(product);
    }

    // Any logged-in user can list a category's products. The category check
    // gives a 404 for a wrong id instead of a misleading empty list.
    public async Task<List<ProductDto>> GetByCategoryAsync(Guid categoryId)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId);
        if (category == null)
            throw new KeyNotFoundException("Category not found");

        var products = await _productRepository.GetByCategoryIdAsync(categoryId);
        return products.Select(ToDto).ToList();
    }

    public async Task<ProductDto> CreateAsync(Guid userId, Guid categoryId, SaveProductDto dto)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId);
        if (category == null || category.UserId != userId)
            throw new KeyNotFoundException("Category not found");

        var name = CleanName(dto);

        if (await _productRepository.ExistsByNameAsync(categoryId, name))
            throw new ArgumentException("This category already has a product with this name");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId
        };
        Apply(product, dto, name);

        await _productRepository.AddAsync(product);
        return ToDto(product);
    }

    public async Task UpdateAsync(Guid userId, Guid productId, SaveProductDto dto)
    {
        var product = await GetOwnedAsync(userId, productId);
        var name = CleanName(dto);

        if (await _productRepository.ExistsByNameAsync(product.CategoryId, name, productId))
            throw new ArgumentException("This category already has a product with this name");

        Apply(product, dto, name);

        if (!await _productRepository.UpdateAsync(product))
            throw new KeyNotFoundException("Product not found");
    }

    public async Task DeleteAsync(Guid userId, Guid productId)
    {
        await GetOwnedAsync(userId, productId);

        if (!await _productRepository.DeleteAsync(productId))
            throw new KeyNotFoundException("Product not found");
    }

    // Loads the product and makes sure its category belongs to the logged-in user.
    private async Task<Product> GetOwnedAsync(Guid userId, Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);

        if (product == null || product.Category == null || product.Category.UserId != userId)
            throw new KeyNotFoundException("Product not found");

        return product;
    }

    private static string CleanName(SaveProductDto dto)
    {
        var name = (dto.Name ?? string.Empty).Trim();

        if (name.Length == 0)
            throw new ArgumentException("Product name is required");

        return name;
    }

    // Copies the editable fields from the dto onto the product (full replace).
    private static void Apply(Product product, SaveProductDto dto, string name)
    {
        product.Name = name;
        product.Price = (dto.Price ?? string.Empty).Trim();
        product.Description = (dto.Description ?? string.Empty).Trim();
        product.Image = (dto.Image ?? string.Empty).Trim();
        product.Discount = dto.Discount;

        if (dto.Quantity > 0)
        {
            product.Quantity = dto.Quantity;
            product.Status = null;
        }
        else
        {
            product.Quantity = null;
            product.Status = dto.Status ?? StockStatus.Available;
        }
    }

    private static ProductDto ToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price ?? string.Empty,
        Description = p.Description ?? string.Empty,
        Image = p.Image ?? string.Empty,
        Quantity = p.Quantity,
        Status = p.Status,
        Discount = p.Discount ?? 0
    };
}