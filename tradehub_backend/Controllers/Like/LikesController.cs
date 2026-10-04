using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Services;

namespace TradeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/posts/{postId:guid}/like")]
public class LikesController : ControllerBase
{
    private readonly ILikeService _likeService;

    public LikesController(ILikeService likeService)
    {
        _likeService = likeService;
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count(Guid postId)
    {
        var count = await _likeService.CountAsync(postId);
        return Ok(count);
    }

    [HttpPut]
    public async Task<IActionResult> Like(Guid postId)
    {
        await _likeService.LikeAsync(GetUserId(), postId);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Unlike(Guid postId)
    {
        await _likeService.UnlikeAsync(GetUserId(), postId);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}