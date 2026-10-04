// Repositories/IOrderRepository.cs
using TradeHub.Models;

namespace TradeHub.Repositories;

public interface IOrderRepository
{
    Task<List<Order>> GetByBuyerIdAsync(Guid buyerId);
    Task<List<Order>> GetBySellerIdAsync(Guid sellerId);
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetDetailsByIdAsync(Guid id);
    Task<Order> AddAsync(Order order);
    Task<bool> UpdateAsync(Order order);
    Task<bool> DeleteAsync(Guid id);
}