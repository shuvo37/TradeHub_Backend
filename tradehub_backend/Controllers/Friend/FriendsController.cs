using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Dtos.Friends;
using TradeHub.Services;

namespace TradeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/friends")]
public class FriendsController : ControllerBase
{
    private readonly IFriendshipService _friendService;
    private readonly ISuggestionService _suggestionService;

    public FriendsController(IFriendshipService friendService, ISuggestionService suggestionService)
    {
        _friendService = friendService;
        _suggestionService = suggestionService;
    }

    // My friends, 10 per page, newest first. 'before' is the NextCursor of the previous page (omit it for the first page).
    [HttpGet]
    public async Task<ActionResult<FriendsPageDto>> GetFriends([FromQuery] DateTimeOffset? before)
    {
        return Ok(await _friendService.GetFriendsAsync(GetUserId(), before));
    }

    // "People you may know": up to 10 people, best score first (no paging: a score has no timestamp to continue from)
    [HttpGet("suggestions")]
    public async Task<ActionResult<List<UserSuggestionDto>>> GetSuggestions()
    {
        return Ok(await _suggestionService.GetSuggestionsAsync(GetUserId()));
    }

    // Unfriend: {userId} is the other person (not a request id)
    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Unfriend(Guid userId)
    {
        await _friendService.UnfriendAsync(GetUserId(), userId);
        return NoContent();
    }

    // How am I connected to this user? (the profile page asks this)
    [HttpGet("status/{userId:guid}")]
    public async Task<ActionResult<FriendStatusDto>> GetStatus(Guid userId)
    {
        return Ok(await _friendService.GetStatusAsync(GetUserId(), userId));
    }

    // Send a friend request to this user
    [HttpPost("requests/{userId:guid}")]
    public async Task<ActionResult<FriendStatusDto>> SendRequest(Guid userId)
    {
        return Ok(await _friendService.SendRequestAsync(GetUserId(), userId));
    }

    // Requests other people sent to me (still waiting for my answer)
    [HttpGet("requests/received")]
    public async Task<ActionResult<List<FriendRequestDto>>> GetReceived()
    {
        return Ok(await _friendService.GetReceivedAsync(GetUserId()));
    }

    // How many waiting requests I have not looked at yet (the red number on the Friends icon)
    [HttpGet("requests/unseen-count")]
    public async Task<ActionResult<int>> GetUnseenCount()
    {
        return Ok(await _friendService.GetUnseenCountAsync(GetUserId()));
    }

    // I opened the Friends page: my waiting requests count as seen (they stay in the list)
    [HttpPut("requests/seen")]
    public async Task<IActionResult> MarkSeen()
    {
        await _friendService.MarkSeenAsync(GetUserId());
        return NoContent();
    }

    // {id} is the request id, not a user id
    [HttpPut("requests/{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id)
    {
        await _friendService.AcceptAsync(GetUserId(), id);
        return NoContent();
    }

    // Decline (receiver) or cancel (sender) a pending request
    [HttpDelete("requests/{id:guid}")]
    public async Task<IActionResult> DeleteRequest(Guid id)
    {
        await _friendService.DeleteRequestAsync(GetUserId(), id);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
