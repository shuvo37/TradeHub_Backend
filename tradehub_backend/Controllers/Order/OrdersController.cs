// Controllers/OrdersController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Dtos.Orders;
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

    [HttpGet("received")]
    public async Task<IActionResult> GetReceived()
    {
        return Ok(await _orderService.GetReceivedAsync(GetUserId()));
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