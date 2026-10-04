using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Dtos.Products;
using TradeHub.Services;

namespace TradeHub.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("categories/{categoryId:guid}/products")]
    public async Task<IActionResult> GetByCategory(Guid categoryId)
    {
        var products = await _productService.GetByCategoryAsync(categoryId);
        return Ok(products);
    }

    [HttpGet("products/{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var product = await _productService.GetAsync(id);
        return Ok(product);
    }

    [HttpPost("categories/{categoryId:guid}/products")]
    public async Task<IActionResult> Create(Guid categoryId, SaveProductDto dto)
    {
        var product = await _productService.CreateAsync(GetUserId(), categoryId, dto);
        return Ok(product);
    }

    [HttpPut("products/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveProductDto dto)
    {
        await _productService.UpdateAsync(GetUserId(), id, dto);
        return NoContent();
    }

    [HttpDelete("products/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _productService.DeleteAsync(GetUserId(), id);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}