// Dtos/Orders/OrderCountsDto.cs
namespace TradeHub.Dtos.Orders;

// How many orders the seller has received, in total and per status.
// Pending is also the number on the bag icon in the top bar.
public class OrderCountsDto
{
    public int All { get; set; }
    public int Pending { get; set; }
    public int Accepted { get; set; }
    public int Rejected { get; set; }
}
