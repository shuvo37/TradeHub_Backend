namespace TradeHub.Dtos.Auth;

public class LoginRequestDto
{
    public string UniqueName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

