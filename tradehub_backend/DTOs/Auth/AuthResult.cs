namespace TradeHub.Dtos.Auth;

// What AuthService hands to the controller: the JSON body, plus the refresh token
// that must go into the cookie (never into the JSON).
public record AuthResult(AuthResponseDto Response, string RefreshToken, DateTime RefreshExpiresAt);