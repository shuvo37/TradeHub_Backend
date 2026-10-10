using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Dtos.Search;
using TradeHub.Services;

namespace TradeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    // Search: products and categories of other sellers that match q, best first.
    // 'skip' = how many products the client already has (leave it out for the first page); use the NextSkip of the last page.
    // Text shorter than 2 letters (or longer than 60) answers 400.
    [HttpGet]
    public async Task<ActionResult<SearchResultDto>> Search([FromQuery] string? q, [FromQuery] int skip = 0)
    {
        return Ok(await _searchService.SearchAsync(GetUserId(), q, skip));
    }

    // The lines under the search box (at most 8, plain texts).
    // ?q= empty or missing: my latest searches. Otherwise: texts that start with q.
    [HttpGet("suggestions")]
    public async Task<ActionResult<List<string>>> GetSuggestions([FromQuery] string? q)
    {
        return Ok(await _searchService.GetSuggestionsAsync(GetUserId(), q));
    }

    // Remember what I searched (call it when I press Enter or click a suggestion, not on every key).
    // Texts shorter than 2 letters answer 400.
    [HttpPost("history")]
    public async Task<IActionResult> SaveHistory(SaveSearchDto dto)
    {
        await _searchService.SaveAsync(GetUserId(), dto);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
