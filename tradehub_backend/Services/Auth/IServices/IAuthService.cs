using TradeHub.Dtos.Auth;

namespace TradeHub.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequestDto request);
    Task<AuthResult?> LoginAsync(LoginRequestDto request);
    Task<AuthResult?> RefreshAsync(string rawRefreshToken);
    Task LogoutAsync(string rawRefreshToken);
}