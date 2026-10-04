using TradeHub.Enums;

namespace TradeHub.Models;

public class Order
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid BuyerId { get; set; }
    public User? Buyer { get; set; }

    public Guid SellerId { get; set; }
    public User? Seller { get; set; }

    public int Quantity { get; set; }
    public int TotalPrice { get; set; }
    public string PickupLocation { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public PaymmentProvider PaymmentProvider { get; set; }

    public string PaidToNumber { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? ProofImage { get; set; }

    public OrderStatus OrderStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}