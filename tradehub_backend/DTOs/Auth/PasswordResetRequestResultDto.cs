namespace TradeHub.Dtos.Auth;

// Answer of POST api/auth/password-reset/request. It is the SAME for every unique name (known or not, with a
// confirmed email or not), so it never reveals anything about an account. The numbers are the fixed rules.
public class PasswordResetRequestResultDto
{
    public int ExpiresInSeconds { get; set; }
    public int ResendInSeconds { get; set; }
}
