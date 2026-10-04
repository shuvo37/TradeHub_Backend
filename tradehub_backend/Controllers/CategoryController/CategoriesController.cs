// Controllers/CategoryController/CategoriesController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Dtos.Categories;
using TradeHub.Services;
namespace TradeHub.Controllers;


[Authorize]
[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _categoryService.GetAllAsync(GetUserId());
        return Ok(categories);
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveCategoryDto dto)
    {
        var category = await _categoryService.CreateAsync(GetUserId(), dto);
        return Ok(category);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveCategoryDto dto)
    {
        await _categoryService.UpdateAsync(GetUserId(), id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _categoryService.DeleteAsync(GetUserId(), id);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}