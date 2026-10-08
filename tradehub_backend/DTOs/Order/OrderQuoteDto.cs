// Dtos/Orders/OrderQuoteDto.cs
namespace TradeHub.Dtos.Orders;

// The price the server would charge for this product and quantity. The order form shows these numbers.
public class OrderQuoteDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }        // one item, before the discount (dollars)
    public decimal DiscountPercent { get; set; }  // 0 when there is no discount
    public int Quantity { get; set; }
    public int TotalPrice { get; set; }           // whole dollars, rounded down
}
