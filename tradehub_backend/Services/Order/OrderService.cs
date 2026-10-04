// Services/OrderService.cs
using TradeHub.Dtos.Orders;
using TradeHub.Enums;
using TradeHub.Models;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task<OrderDto> CreateAsync(Guid buyerId, OrderSaveDto dto)
    {
        // GetByIdAsync includes the Category, so the seller is product.Category.UserId
        var product = await _productRepository.GetByIdAsync(dto.ProductId);
        if (product == null || product.Category == null)
            throw new KeyNotFoundException("Product not found");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            BuyerId = buyerId,
            SellerId = product.Category.UserId,
            Quantity = dto.Quantity,
            TotalPrice = dto.TotalPrice,
            PickupLocation = dto.PickupLocation.Trim(),
            Phone = dto.Phone.Trim(),
            PaymentMethod = dto.PaymentMethod,
            PaymmentProvider = dto.PaymmentProvider,
            PaidToNumber = dto.PaidToNumber.Trim(),
            TransactionId = dto.TransactionId?.Trim(),
            ProofImage = dto.ProofImage?.Trim(),
            OrderStatus = OrderStatus.PENDING,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _orderRepository.AddAsync(order);

        // Reload with product, buyer and seller so the dto has their names
        var saved = await _orderRepository.GetDetailsByIdAsync(order.Id);
        return ToDto(saved!);
    }

    public async Task<List<OrderDto>> GetPlacedAsync(Guid buyerId)
    {
        var orders = await _orderRepository.GetByBuyerIdAsync(buyerId);
        return orders.Select(ToDto).ToList();
    }

    public async Task<List<OrderDto>> GetReceivedAsync(Guid sellerId)
    {
        var orders = await _orderRepository.GetBySellerIdAsync(sellerId);
        return orders.Select(ToDto).ToList();
    }

    public async Task UpdateStatusAsync(Guid sellerId, Guid orderId, UpdateOrderStatusDto dto)
    {
        var order = await GetOwnedBySellerAsync(sellerId, orderId);

        if (order.OrderStatus != OrderStatus.PENDING)
            throw new ArgumentException("This order has already been decided");

        if (dto.OrderStatus != OrderStatus.ACCEPTED && dto.OrderStatus != OrderStatus.REJECTED)
            throw new ArgumentException("Status must be ACCEPTED or REJECTED");

        order.OrderStatus = dto.OrderStatus;

        if (!await _orderRepository.UpdateAsync(order))
            throw new KeyNotFoundException("Order not found");
    }

    public async Task DeleteAsync(Guid sellerId, Guid orderId)
    {
        var order = await GetOwnedBySellerAsync(sellerId, orderId);

        if (order.OrderStatus == OrderStatus.PENDING)
            throw new ArgumentException("Accept or reject the order before deleting it");

        if (!await _orderRepository.DeleteAsync(orderId))
            throw new KeyNotFoundException("Order not found");
    }

    // Only the seller of the order may change or delete it.
    // Anyone else gets the same 404 as a missing order.
    private async Task<Order> GetOwnedBySellerAsync(Guid sellerId, Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);

        if (order == null || order.SellerId != sellerId)
            throw new KeyNotFoundException("Order not found");

        return order;
    }

    private static OrderDto ToDto(Order o) => new()
    {
        Id = o.Id,
        ProductId = o.ProductId,
        ProductName = o.Product?.Name ?? string.Empty,
        ProductImage = o.Product?.Image ?? string.Empty,
        BuyerId = o.BuyerId,
        BuyerName = o.Buyer?.Name ?? string.Empty,
        SellerId = o.SellerId,
        SellerName = o.Seller?.Name ?? string.Empty,
        Quantity = o.Quantity,
        TotalPrice = o.TotalPrice,
        PickupLocation = o.PickupLocation,
        Phone = o.Phone,
        PaymentMethod = o.PaymentMethod,
        PaymmentProvider = o.PaymmentProvider,
        PaidToNumber = o.PaidToNumber,
        TransactionId = o.TransactionId ?? string.Empty,
        ProofImage = o.ProofImage ?? string.Empty,
        OrderStatus = o.OrderStatus,
        CreatedAt = o.CreatedAt
    };
}