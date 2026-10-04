using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly TradeHubDbContext _context;

    public ProductRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Product>> GetByCategoryIdAsync(Guid categoryId)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == categoryId)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<bool> ExistsByNameAsync(Guid categoryId, string name, Guid? excludeId = null)
    {
        return await _context.Products.AnyAsync(p =>
            p.CategoryId == categoryId &&
            p.Name == name &&
            (excludeId == null || p.Id != excludeId));
    }

    public async Task<Product> AddAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        var existing = await _context.Products.FindAsync(product.Id);
        if (existing == null) return false;

        _context.Entry(existing).CurrentValues.SetValues(product);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Products.FindAsync(id);
        if (existing == null) return false;

        _context.Products.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}