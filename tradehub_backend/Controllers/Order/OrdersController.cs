// Controllers/OrdersController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Dtos.Orders;
using TradeHub.Enums;
using TradeHub.Services;

namespace TradeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // The order form asks for the real unit price, discount and total (and gets an error for own product / no stock)
    [HttpGet("quote")]
    public async Task<IActionResult> Quote([FromQuery] Guid productId, [FromQuery] int quantity)
    {
        return Ok(await _orderService.QuoteAsync(GetUserId(), productId, quantity));
    }

    [HttpPost]
    public async Task<IActionResult> Create(OrderSaveDto dto)
    {
        var order = await _orderService.CreateAsync(GetUserId(), dto);
        return Ok(order);
    }

    [HttpGet("placed")]
    public async Task<IActionResult> GetPlaced()
    {
        return Ok(await _orderService.GetPlacedAsync(GetUserId()));
    }

    // One order I placed: the buyer's summary card (opened from the bell). Only the buyer gets it.
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPlacedById(Guid id)
    {
        return Ok(await _orderService.GetPlacedByIdAsync(GetUserId(), id));
    }

    // Orders I received, 10 per page, newest first.
    // Optional: status=PENDING|ACCEPTED|REJECTED, phone=<part of a phone number>.
    // 'before' is the NextCursor of the previous page (omit it for the first page).
    [HttpGet("received")]
    public async Task<IActionResult> GetReceived(
        [FromQuery] OrderStatus? status, [FromQuery] string? phone, [FromQuery] DateTimeOffset? before)
    {
        return Ok(await _orderService.GetReceivedAsync(GetUserId(), status, phone, before));
    }

    // Totals for the filter tabs and the number on the bag icon (Pending)
    [HttpGet("received/counts")]
    public async Task<IActionResult> GetReceivedCounts()
    {
        return Ok(await _orderService.GetReceivedCountsAsync(GetUserId()));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateOrderStatusDto dto)
    {
        await _orderService.UpdateStatusAsync(GetUserId(), id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _orderService.DeleteAsync(GetUserId(), id);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}