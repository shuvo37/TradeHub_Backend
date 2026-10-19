using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeHub.Dtos.ContactEmail;
using TradeHub.Services;

namespace TradeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/contact-email")]
public class ContactEmailController : ControllerBase
{
    private readonly IContactEmailService _contactEmail;

    public ContactEmailController(IContactEmailService contactEmail)
    {
        _contactEmail = contactEmail;
    }

    // Where I stand: no email yet ({ email: null }) -> the form; saved but not verified -> the code step; verified -> the app.
    [HttpGet]
    public async Task<ActionResult<ContactEmailDto>> Get()
    {
        return Ok(await _contactEmail.GetAsync(GetUserId()));
    }

    // Save the email (first time or a change) and mail a 6-digit code to it. A bad email, a domain that cannot
    // receive mail, or too many codes answers 400 with a message.
    [HttpPut]
    public async Task<ActionResult<ContactEmailDto>> Save(SaveContactEmailDto dto)
    {
        return Ok(await _contactEmail.SaveAsync(GetUserId(), dto));
    }

    // A new code for the saved email (the old code stops working). Allowed 60 seconds after the last one.
    [HttpPost("resend")]
    public async Task<ActionResult<ContactEmailDto>> Resend()
    {
        return Ok(await _contactEmail.ResendAsync(GetUserId()));
    }

    // The 6 digits from the mail. Right: the email is verified (204). Wrong, expired or out of tries: 400 with a message.
    [HttpPost("verify")]
    public async Task<IActionResult> Verify(VerifyContactEmailDto dto)
    {
        await _contactEmail.VerifyAsync(GetUserId(), dto);
        return NoContent();
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
