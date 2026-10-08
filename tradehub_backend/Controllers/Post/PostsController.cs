using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Dtos.Posts;
using TradeHub.Services;

namespace TradeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/posts")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    [HttpGet("user/{userId:guid}/allPost")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        var posts = await _postService.GetByUserAsync(GetUserId(), userId);
        return Ok(posts);
    }

    // News feed: my posts and my friends' posts, newest first, 10 per page (a revived post counts from its revive time).
    // 'before' = the nextCursor of the page the client already has (leave it out for the first page).
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] DateTimeOffset? before)
    {
        var page = await _postService.GetFeedAsync(GetUserId(), before);
        return Ok(page);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var post = await _postService.GetAsync(GetUserId(), id);
        return Ok(post);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePostDto dto)
    {
        var post = await _postService.CreateAsync(GetUserId(), dto);
        return Ok(post);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateText(Guid id, UpdatePostDto dto)
    {
        await _postService.UpdateTextAsync(GetUserId(), id, dto);
        return NoContent();
    }

    // Moves my post back to the top of the feed. Once per 24 hours per post (editing shares the same wait);
    // too early answers 400 with the time left.
    [HttpPut("{id:guid}/revive")]
    public async Task<IActionResult> Revive(Guid id)
    {
        await _postService.ReviveAsync(GetUserId(), id);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _postService.DeleteAsync(GetUserId(), id);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}