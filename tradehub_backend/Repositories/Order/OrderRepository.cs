// Repositories/OrderRepository.cs
using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly TradeHubDbContext _context;

    public OrderRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    // Read-only query with product, buyer and seller loaded (for building OrderDto)
    private IQueryable<Order> WithDetails()
    {
        return _context.Orders
            .AsNoTracking()
            .Include(o => o.Product)
            .Include(o => o.Buyer)
            .Include(o => o.Seller);
    }

    public async Task<List<Order>> GetByBuyerIdAsync(Guid buyerId)
    {
        return await WithDetails()
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Order>> GetBySellerIdAsync(Guid sellerId)
    {
        return await WithDetails()
            .Where(o => o.SellerId == sellerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders.FindAsync(id);
    }

    public async Task<Order?> GetDetailsByIdAsync(Guid id)
    {
        return await WithDetails().FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order> AddAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<bool> UpdateAsync(Order order)
    {
        var existing = await _context.Orders.FindAsync(order.Id);
        if (existing == null) return false;

        _context.Entry(existing).CurrentValues.SetValues(order);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Orders.FindAsync(id);
        if (existing == null) return false;

        _context.Orders.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}