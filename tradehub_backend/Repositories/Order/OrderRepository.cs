// Repositories/OrderRepository.cs
using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Enums;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly TradeHubDbContext _context;

    public OrderRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    // Read-only query with buyer and seller loaded (for building OrderDto).
    // The product is not loaded: the order keeps its own copy of the product's name, image and price.
    private IQueryable<Order> WithDetails()
    {
        return _context.Orders
            .AsNoTracking()
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

    public async Task<List<Order>> GetReceivedPageAsync(
        Guid sellerId, OrderStatus? status, string? phone, DateTimeOffset? before, int take)
    {
        var query = WithDetails().Where(o => o.SellerId == sellerId);

        if (status != null)
            query = query.Where(o => o.OrderStatus == status.Value);

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var text = phone.Trim();
            query = query.Where(o => o.Phone.Contains(text));
        }

        if (before != null)
            query = query.Where(o => o.CreatedAt < before.Value);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Dictionary<OrderStatus, int>> GetReceivedCountsAsync(Guid sellerId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.SellerId == sellerId)
            .GroupBy(o => o.OrderStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders.FindAsync(id);
    }

    public async Task<Order?> GetDetailsByIdAsync(Guid id)
    {
        return await WithDetails().FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<bool> AddAsync(Order order, bool reduceStock)
    {
        // Stock and order are saved together: if anything fails, both are rolled back
        await using var transaction = await _context.Database.BeginTransactionAsync();

        if (reduceStock)
        {
            var productId = order.ProductId;
            var quantity = order.Quantity;

            // One UPDATE that checks and subtracts at the same moment, so two buyers can never take the last item.
            // Zero rows changed means not enough stock is left (or the product no longer counts stock).
            var changed = await _context.Products
                .Where(p => p.Id == productId && p.Quantity != null && p.Quantity >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Quantity, p => p.Quantity - quantity));

            if (changed == 0)
                return false;
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> DecideAsync(Guid orderId, OrderStatus status)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Only a PENDING order can be decided. Two quick clicks cannot both succeed:
        // the second one finds the order already decided and changes zero rows.
        var changed = await _context.Orders
            .Where(o => o.Id == orderId && o.OrderStatus == OrderStatus.PENDING)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.OrderStatus, status));

        if (changed == 0)
            return false;

        // A rejected order gives its items back to the product (only if the product still counts stock)
        if (status == OrderStatus.REJECTED)
        {
            var order = await _context.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);

            if (order.ProductId != null)
            {
                var productId = order.ProductId;
                var quantity = order.Quantity;

                await _context.Products
                    .Where(p => p.Id == productId && p.Quantity != null)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Quantity, p => p.Quantity + quantity));
            }
        }

        await transaction.CommitAsync();
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