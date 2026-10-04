// Dtos/Orders/UpdateOrderStatusDto.cs
using TradeHub.Enums;

namespace TradeHub.Dtos.Orders;

public class UpdateOrderStatusDto
{
    public OrderStatus OrderStatus { get; set; }
}