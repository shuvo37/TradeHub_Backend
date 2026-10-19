using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradeHub.Dtos.Auth;
using TradeHub.Services;

namespace TradeHub.Controllers;

// No login needed (the person forgot the password). Each IP address gets only a few calls (policy "PasswordReset" in Program.cs).
[ApiController]
[EnableRateLimiting("PasswordReset")]
[Route("api/auth/password-reset")]
public class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _passwordReset;

    public PasswordResetController(IPasswordResetService passwordReset)
    {
        _passwordReset = passwordReset;
    }

    // Step 1: the unique name. A 6-digit code is mailed to the account's confirmed email.
    // The answer is the same whether the account exists or not.
    [HttpPost("request")]
    public async Task<ActionResult<PasswordResetRequestResultDto>> RequestCode(PasswordResetRequestDto dto)
    {
        return Ok(await _passwordReset.RequestAsync(dto.UniqueName));
    }

    // Step 2: unique name + the code + the new password. Right: the password is changed (204).
    // Wrong, expired or out of tries: 400 with a message.
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(PasswordResetConfirmDto dto)
    {
        await _passwordReset.ConfirmAsync(dto);
        return NoContent();
    }
}
