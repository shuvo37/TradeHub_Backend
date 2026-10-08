// Dtos/Orders/OrderDto.cs
using TradeHub.Enums;

namespace TradeHub.Dtos.Orders;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }  // null if the seller deleted the product
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public Guid BuyerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int Quantity { get; set; }
    public int TotalPrice { get; set; }
    public string PickupLocation { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public PaymmentProvider? PaymmentProvider { get; set; }
    public string PaidToNumber { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string ProofImage { get; set; } = string.Empty;
    public OrderStatus OrderStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
