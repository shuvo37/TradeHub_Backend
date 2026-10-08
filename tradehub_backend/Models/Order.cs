using TradeHub.Enums;

namespace TradeHub.Models;

public class Order
{
    public Guid Id { get; set; }

    // Null after the seller deletes the product: the order stays, because it keeps its own copy below
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid BuyerId { get; set; }
    public User? Buyer { get; set; }

    public Guid SellerId { get; set; }
    public User? Seller { get; set; }

    // Copied from the product when the order is placed. Editing or deleting the product later never changes the order.
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }        // list price of one item, before the discount (dollars)
    public decimal DiscountPercent { get; set; }  // 0 when there was no discount

    public int Quantity { get; set; }
    public int TotalPrice { get; set; }           // calculated by the server, whole dollars (rounded down)
    public string PickupLocation { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public PaymmentProvider? PaymmentProvider { get; set; } // only for "pay before delivery"

    public string PaidToNumber { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? ProofImage { get; set; }

    public OrderStatus OrderStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // When the seller accepted or rejected the order (server time). Null while it is PENDING.
    // The buyer's summary card shows it as proof of the seller's decision.
    public DateTimeOffset? DecidedAt { get; set; }

    // The seller "deleted" the order: it disappears from the seller's list and counts, but the row stays,
    // because the buyer keeps it as proof of what the seller decided.
    public bool DeletedBySeller { get; set; }
}
