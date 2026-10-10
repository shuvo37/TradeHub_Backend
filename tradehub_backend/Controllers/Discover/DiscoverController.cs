using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Dtos.Posts;
using TradeHub.Services;

namespace TradeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/discover")]
public class DiscoverController : ControllerBase
{
    private readonly IDiscoverService _discoverService;

    public DiscoverController(IDiscoverService discoverService)
    {
        _discoverService = discoverService;
    }

    // Discover "For you": posts from people outside my friends list, best score first, 10 per page.
    // 'skip' = how many posts the client already has (leave it out for the first page); use the NextSkip of the last page.
    [HttpGet("posts")]
    public async Task<ActionResult<DiscoverPageDto>> GetPosts([FromQuery] int skip = 0)
    {
        return Ok(await _discoverService.GetPostsAsync(GetUserId(), skip));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
