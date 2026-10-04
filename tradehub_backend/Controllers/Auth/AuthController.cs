using Microsoft.AspNetCore.Mvc;
using TradeHub.Dtos.Auth;
using TradeHub.Services;

namespace TradeHub.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshCookie = "refresh_token";
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Name and password are required.");

        var result = await _authService.RegisterAsync(request);
        SetRefreshCookie(result);
        return CreatedAtAction(nameof(Register), new { id = result.Response.Id }, result.Response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        if (result is null) return Unauthorized("Invalid unique name or password.");

        SetRefreshCookie(result);
        return Ok(result.Response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh()
    {
        var raw = Request.Cookies[RefreshCookie];
        if (string.IsNullOrEmpty(raw)) return Unauthorized();

        var result = await _authService.RefreshAsync(raw);
        if (result is null)
        {
            Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/auth" });
            return Unauthorized();
        }

        SetRefreshCookie(result);
        return Ok(result.Response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var raw = Request.Cookies[RefreshCookie];
        if (!string.IsNullOrEmpty(raw)) await _authService.LogoutAsync(raw);

        Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

    private void SetRefreshCookie(AuthResult result) =>
        Response.Cookies.Append(RefreshCookie, result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,               // JavaScript can't read it, so XSS can't steal it
            Secure = Request.IsHttps,      // true automatically in production (https)
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",            // the browser only sends it to auth endpoints
            Expires = result.RefreshExpiresAt,
        });
}