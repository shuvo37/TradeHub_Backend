using TradeHub.Enums;

namespace TradeHub.Services;

// Where a user stands with the current code: how long it still works and how long until a new one can be asked.
// Both are in seconds counted by the server (no clock of the browser is involved); 0 = none / allowed now.
public record EmailCodeStatus(int ExpiresInSeconds, int ResendInSeconds);

// Sends confirmation codes by email and checks what the user typed. It knows nothing about WHAT the code is for:
// contact email verification uses it now, "forgot password" will use it with its own EmailCodePurpose.
public interface IEmailCodeService
{
    // The fixed rules, so a screen that must not reveal anything about an account can still show the countdowns
    int CodeLifetimeSeconds { get; }
    int ResendWaitSeconds { get; }

    // Makes a new 6-digit code (the old one stops working) and mails it. Throws ArgumentException (400) with a
    // message when the user must wait, asked too often, or the mail could not be sent.
    Task SendAsync(Guid userId, string userName, EmailCodePurpose purpose, string email);

    // Checks the typed code. Returns normally when it is right (and the code is used up).
    // Throws ArgumentException (400) with a message when it is wrong, expired or out of tries.
    Task VerifyAsync(Guid userId, EmailCodePurpose purpose, string email, string? code);

    Task<EmailCodeStatus> GetStatusAsync(Guid userId, EmailCodePurpose purpose, string email);
}
