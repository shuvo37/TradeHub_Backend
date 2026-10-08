// Services/OrderService.cs
using System.Globalization;
using System.Text.RegularExpressions;
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

    // The order form calls this to show the real numbers. It runs the same checks as CreateAsync
    // (own product, stock, price), so the buyer sees a problem before filling in the details.
    public async Task<OrderQuoteDto> QuoteAsync(Guid buyerId, Guid productId, int quantity)
    {
        var (_, quote) = await PrepareAsync(buyerId, productId, quantity);
        return quote;
    }

    public async Task<OrderDto> CreateAsync(Guid buyerId, OrderSaveDto dto)
    {
        var phone = (dto.Phone ?? string.Empty).Trim();
        var pickupLocation = (dto.PickupLocation ?? string.Empty).Trim();

        if (dto.Quantity < 1)
            throw new ArgumentException("Quantity must be at least 1");
        if (pickupLocation.Length == 0)
            throw new ArgumentException("Pickup location is required");
        if (phone.Length == 0)
            throw new ArgumentException("Phone number is required");
        if (!Enum.IsDefined(dto.PaymentMethod))
            throw new ArgumentException("Payment method is not valid");

        var prepaid = dto.PaymentMethod == PaymentMethod.PAYMENT_BEFORE_DELIVARY;
        var transactionId = (dto.TransactionId ?? string.Empty).Trim();
        var proofImage = (dto.ProofImage ?? string.Empty).Trim();

        if (prepaid)
        {
            if (dto.PaymmentProvider == null || !Enum.IsDefined(dto.PaymmentProvider.Value))
                throw new ArgumentException("Choose bKash or Nagad");

            // The buyer must give at least one of the two. The seller looks at them and decides.
            if (transactionId.Length == 0 && proofImage.Length == 0)
                throw new ArgumentException("Add a transaction ID or a payment screenshot");
        }

        var (product, quote) = await PrepareAsync(buyerId, dto.ProductId, dto.Quantity);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            BuyerId = buyerId,
            SellerId = product.Category!.UserId,

            // Own copy of what the buyer saw, so later product edits or deletion never change this order
            ProductName = product.Name,
            ProductImage = product.Image ?? string.Empty,
            CategoryName = product.Category.Name,
            UnitPrice = quote.UnitPrice,
            DiscountPercent = quote.DiscountPercent,

            Quantity = dto.Quantity,
            TotalPrice = quote.TotalPrice,
            PickupLocation = pickupLocation,
            Phone = phone,
            PaymentMethod = dto.PaymentMethod,

            // Payment details are kept only for "pay before delivery"; cash on delivery ignores them
            PaymmentProvider = prepaid ? dto.PaymmentProvider : null,
            PaidToNumber = prepaid ? (dto.PaidToNumber ?? string.Empty).Trim() : string.Empty,
            TransactionId = prepaid && transactionId.Length > 0 ? transactionId : null,
            ProofImage = prepaid && proofImage.Length > 0 ? proofImage : null,

            OrderStatus = OrderStatus.PENDING,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // A product that counts stock has it reduced together with saving the order.
        // The repository does the check and the subtraction in one step, so there is no overselling.
        var reduceStock = product.Quantity != null;

        if (!await _orderRepository.AddAsync(order, reduceStock))
            throw new ArgumentException("Not enough stock left");

        var saved = await _orderRepository.GetDetailsByIdAsync(order.Id);
        return ToDto(saved!);
    }

    public async Task<List<OrderDto>> GetPlacedAsync(Guid buyerId)
    {
        var orders = await _orderRepository.GetByBuyerIdAsync(buyerId);
        return orders.Select(ToDto).ToList();
    }

    private const int ReceivedPageSize = 10;

    public async Task<OrderPageDto> GetReceivedAsync(
        Guid sellerId, OrderStatus? status, string? phone, DateTimeOffset? before)
    {
        // Ask for one extra row: if it comes back, there is a next page
        var rows = await _orderRepository.GetReceivedPageAsync(sellerId, status, phone, before, ReceivedPageSize + 1);
        var page = rows.Take(ReceivedPageSize).ToList();

        DateTimeOffset? nextCursor = null;
        if (page.Count > 0)
            nextCursor = page[^1].CreatedAt;

        return new OrderPageDto
        {
            Items = page.Select(ToDto).ToList(),
            HasMore = rows.Count > ReceivedPageSize,
            NextCursor = nextCursor
        };
    }

    public async Task<OrderCountsDto> GetReceivedCountsAsync(Guid sellerId)
    {
        var counts = await _orderRepository.GetReceivedCountsAsync(sellerId);

        int Of(OrderStatus s) => counts.TryGetValue(s, out var n) ? n : 0;

        return new OrderCountsDto
        {
            All = counts.Values.Sum(),
            Pending = Of(OrderStatus.PENDING),
            Accepted = Of(OrderStatus.ACCEPTED),
            Rejected = Of(OrderStatus.REJECTED)
        };
    }

    public async Task UpdateStatusAsync(Guid sellerId, Guid orderId, UpdateOrderStatusDto dto)
    {
        var order = await GetOwnedBySellerAsync(sellerId, orderId);

        if (order.OrderStatus != OrderStatus.PENDING)
            throw new ArgumentException("This order has already been decided");

        if (dto.OrderStatus != OrderStatus.ACCEPTED && dto.OrderStatus != OrderStatus.REJECTED)
            throw new ArgumentException("Status must be ACCEPTED or REJECTED");

        // Rejecting also gives the stock back (done inside the repository, in one transaction)
        if (!await _orderRepository.DecideAsync(orderId, dto.OrderStatus))
            throw new ArgumentException("This order has already been decided");
    }

    public async Task DeleteAsync(Guid sellerId, Guid orderId)
    {
        var order = await GetOwnedBySellerAsync(sellerId, orderId);

        if (order.OrderStatus == OrderStatus.PENDING)
            throw new ArgumentException("Accept or reject the order before deleting it");

        if (!await _orderRepository.DeleteAsync(orderId))
            throw new KeyNotFoundException("Order not found");
    }

    // Everything that decides whether this buyer may order this product, and what it costs.
    // Used by both the quote and the real order, so they can never disagree.
    private async Task<(Product Product, OrderQuoteDto Quote)> PrepareAsync(Guid buyerId, Guid productId, int quantity)
    {
        if (quantity < 1)
            throw new ArgumentException("Quantity must be at least 1");

        // GetByIdAsync includes the Category, so the seller is product.Category.UserId
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null || product.Category == null)
            throw new KeyNotFoundException("Product not found");

        if (product.Category.UserId == buyerId)
            throw new ArgumentException("You can't order your own product");

        if (product.Quantity != null)
        {
            // The product counts its stock: never sell more than is left
            if (product.Quantity <= 0)
                throw new ArgumentException("This product is out of stock");
            if (quantity > product.Quantity)
                throw new ArgumentException($"Only {product.Quantity} left in stock");
        }
        else if (product.Status == StockStatus.Unavailable)
        {
            // The product only has Available / Unavailable: nothing to count
            throw new ArgumentException("This product is unavailable");
        }

        var unitPrice = ParsePrice(product.Price);
        var discount = Math.Clamp(product.Discount ?? 0m, 0m, 100m);

        // Whole dollars, rounded down: 3 x $15.83 with 10% off = 42.741 -> 42
        var total = Math.Floor(unitPrice * quantity * (100m - discount) / 100m);

        if (total > int.MaxValue)
            throw new ArgumentException("The order total is too large");

        var quote = new OrderQuoteDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = unitPrice,
            DiscountPercent = discount,
            Quantity = quantity,
            TotalPrice = (int)total
        };

        return (product, quote);
    }

    // The product price is text such as "$50", "50" or "$1,200.50". Take the number out of it.
    private static decimal ParsePrice(string? price)
    {
        var match = Regex.Match(price ?? string.Empty, @"\d[\d,]*(\.\d+)?");

        if (!match.Success ||
            !decimal.TryParse(match.Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ||
            value <= 0)
            throw new ArgumentException("This product has no valid price");

        return value;
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
        ProductName = o.ProductName,
        ProductImage = o.ProductImage,
        CategoryName = o.CategoryName,
        BuyerId = o.BuyerId,
        BuyerName = o.Buyer?.Name ?? string.Empty,
        SellerId = o.SellerId,
        SellerName = o.Seller?.Name ?? string.Empty,
        UnitPrice = o.UnitPrice,
        DiscountPercent = o.DiscountPercent,
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
