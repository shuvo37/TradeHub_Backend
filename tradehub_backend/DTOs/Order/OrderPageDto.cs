// Dtos/Orders/OrderPageDto.cs
namespace TradeHub.Dtos.Orders;

// One page of the seller's received orders (newest first).
// HasMore tells the client whether the "More" button is needed.
public class OrderPageDto
{
    public List<OrderDto> Items { get; set; } = new();
    public bool HasMore { get; set; }

    // Where the next page starts: the CreatedAt of the last order in Items.
    // The client sends it back unchanged as ?before=... when "More" is pressed. null when the page is empty.
    public DateTimeOffset? NextCursor { get; set; }
}
