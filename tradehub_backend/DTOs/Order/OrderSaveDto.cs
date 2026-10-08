// Dtos/Orders/OrderSaveDto.cs
using TradeHub.Enums;

namespace TradeHub.Dtos.Orders;

// What the buyer sends. There is no price here on purpose: the server calculates the total.
public class OrderSaveDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string PickupLocation { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public PaymmentProvider? PaymmentProvider { get; set; } // required only for PAYMENT_BEFORE_DELIVARY
    public string PaidToNumber { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? ProofImage { get; set; }
}
