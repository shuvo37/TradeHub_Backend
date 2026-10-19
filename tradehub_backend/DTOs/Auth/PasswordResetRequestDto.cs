namespace TradeHub.Dtos.Auth;

// Body of POST api/auth/password-reset/request: whose password was forgotten
public class PasswordResetRequestDto
{
    // Nullable on purpose: a missing value reaches the service, which answers "Enter your unique name."
    public string? UniqueName { get; set; }
}
