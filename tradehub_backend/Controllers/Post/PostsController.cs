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
        var posts = await _postService.GetByUserAsync(userId);
        return Ok(posts);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var post = await _postService.GetAsync(id);
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _postService.DeleteAsync(GetUserId(), id);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}