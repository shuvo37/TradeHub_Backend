using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradeHub.Dtos.Ai;
using TradeHub.Services;

namespace TradeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiAnalystService _analyst;

    public AiController(IAiAnalystService analyst)
    {
        _analyst = analyst;
    }

    // The question (and the chat memory kept in the browser) goes in, an answer based on the user's live data comes out.
    // Nothing is saved: the server forgets the chat as soon as it answers.
    [EnableRateLimiting("Ai")]
    [HttpPost("ask")]
    public async Task<ActionResult<AiAskResponse>> Ask(AiAskRequest request)
    {
        return Ok(await _analyst.AskAsync(GetUserId(), request.Question, request.History));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
