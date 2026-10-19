namespace TradeHub.Dtos.Auth;

// Body of POST api/auth/password-reset/confirm: the code from the mail and the new password
public class PasswordResetConfirmDto
{
    public string? UniqueName { get; set; }
    public string? Code { get; set; }
    public string? NewPassword { get; set; }
}
