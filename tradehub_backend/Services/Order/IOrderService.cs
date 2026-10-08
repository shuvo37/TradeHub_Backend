// Services/IOrderService.cs
using TradeHub.Dtos.Orders;
using TradeHub.Enums;

namespace TradeHub.Services;

public interface IOrderService
{
    Task<OrderQuoteDto> QuoteAsync(Guid buyerId, Guid productId, int quantity);
    Task<OrderDto> CreateAsync(Guid buyerId, OrderSaveDto dto);
    Task<List<OrderDto>> GetPlacedAsync(Guid buyerId);
    // One order I placed (the buyer's summary card). Someone else's order is the same 404 as a missing one.
    Task<OrderDto> GetPlacedByIdAsync(Guid buyerId, Guid orderId);
    // One page (10) of the orders I received, newest first; 'before' is the NextCursor of the previous page
    Task<OrderPageDto> GetReceivedAsync(Guid sellerId, OrderStatus? status, string? phone, DateTimeOffset? before);
    Task<OrderCountsDto> GetReceivedCountsAsync(Guid sellerId);
    Task UpdateStatusAsync(Guid sellerId, Guid orderId, UpdateOrderStatusDto dto);
    Task DeleteAsync(Guid sellerId, Guid orderId);
}
