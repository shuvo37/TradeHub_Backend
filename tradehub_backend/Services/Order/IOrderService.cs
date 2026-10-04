// Services/IOrderService.cs
using TradeHub.Dtos.Orders;

namespace TradeHub.Services;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(Guid buyerId, OrderSaveDto dto);
    Task<List<OrderDto>> GetPlacedAsync(Guid buyerId);
    Task<List<OrderDto>> GetReceivedAsync(Guid sellerId);
    Task UpdateStatusAsync(Guid sellerId, Guid orderId, UpdateOrderStatusDto dto);
    Task DeleteAsync(Guid sellerId, Guid orderId);
}