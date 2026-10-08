// Repositories/IOrderRepository.cs
using TradeHub.Enums;
using TradeHub.Models;

namespace TradeHub.Repositories;

public interface IOrderRepository
{
    Task<List<Order>> GetByBuyerIdAsync(Guid buyerId);

    // Orders the seller received, newest first. status and phone are optional filters (phone = "contains").
    // before is the CreatedAt of the last order of the previous page. The caller asks for one extra row (take = page size + 1).
    Task<List<Order>> GetReceivedPageAsync(Guid sellerId, OrderStatus? status, string? phone, DateTimeOffset? before, int take);

    // Number of received orders per status (statuses with no orders are missing from the result)
    Task<Dictionary<OrderStatus, int>> GetReceivedCountsAsync(Guid sellerId);

    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetDetailsByIdAsync(Guid id);

    // Saves the order. When reduceStock is true it first takes order.Quantity from the product's stock,
    // in the same transaction, and only if enough is left. Returns false (nothing saved) when not enough is left.
    Task<bool> AddAsync(Order order, bool reduceStock);

    // Moves a PENDING order to ACCEPTED or REJECTED. A rejected order gives its quantity back to the product's stock.
    // Returns false when the order is missing or was already decided (nothing changed).
    Task<bool> DecideAsync(Guid orderId, OrderStatus status);

    Task<bool> DeleteAsync(Guid id);
}
