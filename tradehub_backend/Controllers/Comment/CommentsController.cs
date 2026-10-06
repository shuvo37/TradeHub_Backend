using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Dtos.Comments;
using TradeHub.Services;

namespace TradeHub.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet("comments/post/{postId:guid}/allComment")]
    public async Task<IActionResult> GetByPost(Guid postId)
    {
        var comments = await _commentService.GetByPostAsync(postId);
        return Ok(comments);
    }

    [HttpGet("comments/post/{postId:guid}/page")]
    public async Task<IActionResult> GetPageByPost(Guid postId, [FromQuery] DateTimeOffset? after)
    {
        var page = await _commentService.GetPageByPostAsync(postId, after);
        return Ok(page);
    }

    [HttpGet("comments/post/{postId:guid}/count")]
    public async Task<IActionResult> CountByPost(Guid postId)
    {
        var count = await _commentService.CountByPostAsync(postId);
        return Ok(count);
    }

    [HttpGet("comments/{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var comment = await _commentService.GetAsync(id);
        return Ok(comment);
    }

    [HttpPost("posts/{postId:guid}/comments")]
    public async Task<IActionResult> Create(Guid postId, CreateCommentDto dto)
    {
        var comment = await _commentService.CreateAsync(GetUserId(), postId, dto);
        return Ok(comment);
    }

    [HttpPut("comments/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCommentDto dto)
    {
        await _commentService.UpdateAsync(GetUserId(), id, dto);
        return NoContent();
    }

    [HttpDelete("comments/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _commentService.DeleteAsync(GetUserId(), id);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
