using TradeHub.Dtos.Auth;

namespace TradeHub.Services;

public interface IPasswordResetService
{
    // Mails a code to the confirmed email of this account. Always answers the same thing (see PasswordResetRequestResultDto).
    Task<PasswordResetRequestResultDto> RequestAsync(string? uniqueName);
    // Checks the code and sets the new password. Throws ArgumentException (400) with a message when something is wrong.
    Task ConfirmAsync(PasswordResetConfirmDto dto);
}
