// Controllers/Users/UsersController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Dtos.Categories;
using TradeHub.Dtos.Users;
using TradeHub.Services;

namespace TradeHub.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ICategoryService _categories;

    public UsersController(IUserService users, ICategoryService categories)
    {
        _users = users;
        _categories = categories;
    }

    // The id always comes from the token, never from the URL or body
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        return Ok(await _users.GetMeAsync(CurrentUserId));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileDto dto)
    {
        return Ok(await _users.UpdateMeAsync(CurrentUserId, dto));
    }

    // Search people: /api/users/search?q=rahim finds every name containing "rahim";
    // /api/users/search?q=Rahim_x7k finds exactly that account (its unique name).
    [HttpGet("search")]
    public async Task<ActionResult<List<UserSummaryDto>>> Search([FromQuery] string? q)
    {
        return Ok(await _users.SearchAsync(CurrentUserId, q));
    }

    // Visiting someone's profile. Any logged-in user can read it (the id here is the person being visited).
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        return Ok(await _users.GetByIdAsync(id));
    }

    // That person's categories with their products, read-only. Checks the user exists first,
    // so an unknown id is a 404 instead of an empty list.
    [HttpGet("{id:guid}/categories")]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories(Guid id)
    {
        await _users.GetByIdAsync(id);
        return Ok(await _categories.GetAllAsync(id));
    }
}
