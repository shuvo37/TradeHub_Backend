using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Dtos.Users;
using TradeHub.Services;

namespace TradeHub.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
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
}