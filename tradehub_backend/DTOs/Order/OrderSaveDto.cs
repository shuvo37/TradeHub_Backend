// Dtos/Orders/OrderSaveDto.cs
using TradeHub.Enums;

namespace TradeHub.Dtos.Orders;

public class OrderSaveDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public int TotalPrice { get; set; }
    public string PickupLocation { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public PaymmentProvider PaymmentProvider { get; set; }
    public string PaidToNumber { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? ProofImage { get; set; }
}