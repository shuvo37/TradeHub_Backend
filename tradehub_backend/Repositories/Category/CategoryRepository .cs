

using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class CategoryRepository : ICategoryRepository
{
    

   private readonly TradeHubDbContext _context;

   public CategoryRepository(TradeHubDbContext  context)
    {
        
            _context = context;

    }


   public async Task<List<Category>> GetAllByUserIdAsync(Guid userId)
    {
        return await _context.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

     public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _context.Categories.FindAsync(id);
    }

   public async Task<bool> ExistsByNameAsync(Guid userId, string name, Guid? excludeId = null)
    {
        return await _context.Categories.AnyAsync(c =>
            c.UserId == userId &&
            c.Name == name &&
            (excludeId == null || c.Id != excludeId));
    }

    public async Task<Category> AddAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

   public async Task<bool> UpdateAsync(Category category)
    {
        var existing = await _context.Categories.FindAsync(category.Id);
        if (existing == null) return false;

        _context.Entry(existing).CurrentValues.SetValues(category);
        await _context.SaveChangesAsync();
        return true;
    }

   public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Categories.FindAsync(id);
        if (existing == null) return false;

        _context.Categories.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }


}