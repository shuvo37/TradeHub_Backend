using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class SearchRepository : ISearchRepository
{
    private readonly TradeHubDbContext _context;

    public SearchRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(Guid userId, string term, DateTimeOffset now)
    {
        // Most searches are repeats, so first try to add one to the existing row (one atomic statement, no read first)
        var updated = await UpdateExistingAsync(userId, term, now);
        if (updated > 0) return;

        _context.SearchHistories.Add(new SearchHistory
        {
            UserId = userId,
            Term = term,
            SearchCount = 1,
            LastSearchedAt = now
        });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The same user saved the same text at the same moment (two tabs, a double click): the other request
            // inserted the row first. Forget our insert and count this search on the row that now exists.
            foreach (var entry in _context.ChangeTracker.Entries<SearchHistory>().ToList())
                entry.State = EntityState.Detached;

            await UpdateExistingAsync(userId, term, now);
        }
    }

    private Task<int> UpdateExistingAsync(Guid userId, string term, DateTimeOffset now)
    {
        return _context.SearchHistories
            .Where(s => s.UserId == userId && s.Term == term)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.SearchCount, x => x.SearchCount + 1)
                .SetProperty(x => x.LastSearchedAt, now));
    }

    public async Task<List<string>> GetRecentAsync(Guid userId, int take)
    {
        return await _context.SearchHistories
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastSearchedAt)
            .ThenBy(s => s.Term)
            .Select(s => s.Term)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<string>> GetOwnByPrefixAsync(Guid userId, string prefix, int take)
    {
        return await _context.SearchHistories
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.Term.StartsWith(prefix))
            .OrderByDescending(s => s.LastSearchedAt)
            .ThenBy(s => s.Term)
            .Select(s => s.Term)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<string>> GetGlobalByPrefixAsync(string prefix, int minUsers, int take)
    {
        // One row per (user, text), so counting the rows of a text counts the different people who searched it
        return await _context.SearchHistories
            .AsNoTracking()
            .Where(s => s.Term.StartsWith(prefix))
            .GroupBy(s => s.Term)
            .Select(g => new { Term = g.Key, Users = g.Count(), Total = g.Sum(x => x.SearchCount) })
            .Where(x => x.Users >= minUsers)
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Term)
            .Select(x => x.Term)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<string>> GetCatalogNamesByPrefixAsync(Guid userId, string prefix, int take)
    {
        // Products and categories of other sellers (my own are never in my search results)
        var productNames = await _context.Products
            .AsNoTracking()
            .Where(p => p.Category!.UserId != userId && p.Name.ToLower().StartsWith(prefix))
            .Select(p => p.Name.ToLower())
            .Distinct()
            .OrderBy(n => n)
            .Take(take)
            .ToListAsync();

        var categoryNames = await _context.Categories
            .AsNoTracking()
            .Where(c => c.UserId != userId && c.Name.ToLower().StartsWith(prefix))
            .Select(c => c.Name.ToLower())
            .Distinct()
            .OrderBy(n => n)
            .Take(take)
            .ToListAsync();

        return productNames
            .Concat(categoryNames)
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct()
            .OrderBy(n => n)
            .Take(take)
            .ToList();
    }

    public async Task<List<ProductSearchRow>> GetProductRowsAsync(Guid userId, string query, List<string> words, int take)
    {
        // A seller's products are the products of the seller's categories
        var products = _context.Products
            .AsNoTracking()
            .Where(p => p.Category!.UserId != userId);

        // Every word has to be somewhere: in the name, in the category name or in the description
        foreach (var word in words)
        {
            var w = word;
            products = products.Where(p =>
                p.Name.ToLower().Contains(w) ||
                p.Category!.Name.ToLower().Contains(w) ||
                (p.Description ?? string.Empty).ToLower().Contains(w));
        }

        return await products
            .OrderByDescending(p => p.Name.ToLower().Contains(query))
            .ThenBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Take(take)
            .Select(p => new ProductSearchRow
            {
                ProductId = p.Id,
                Name = p.Name,
                Price = p.Price ?? string.Empty,
                Description = p.Description ?? string.Empty,
                Image = p.Image ?? string.Empty,
                Quantity = p.Quantity,
                Status = p.Status,
                Discount = p.Discount ?? 0,
                CategoryId = p.CategoryId,
                CategoryName = p.Category!.Name,
                SellerId = p.Category!.UserId,
                SellerName = p.Category!.User!.Name,
                SellerAvatar = p.Category!.User!.Avatar ?? string.Empty
            })
            .ToListAsync();
    }

    public async Task<List<CategorySearchRow>> GetCategoryRowsAsync(Guid userId, string query, List<string> words, int take)
    {
        // A category with no products has nothing to show, so it is left out
        var categories = _context.Categories
            .AsNoTracking()
            .Where(c => c.UserId != userId && c.Products.Any());

        foreach (var word in words)
        {
            var w = word;
            categories = categories.Where(c => c.Name.ToLower().Contains(w));
        }

        return await categories
            .OrderByDescending(c => c.Name.ToLower().Contains(query))
            .ThenBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Take(take)
            .Select(c => new CategorySearchRow
            {
                CategoryId = c.Id,
                Name = c.Name,
                SellerId = c.UserId,
                SellerName = c.User!.Name,
                SellerAvatar = c.User!.Avatar ?? string.Empty,
                ProductCount = c.Products.Count
            })
            .ToListAsync();
    }
}
