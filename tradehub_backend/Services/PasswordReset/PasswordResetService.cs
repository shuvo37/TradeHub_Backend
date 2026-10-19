using TradeHub.Dtos.Auth;
using TradeHub.Enums;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class PasswordResetService : IPasswordResetService
{
    // ---- The rules ----
    //
    //   1) The account is found by its unique name (the same email can sit on many accounts, so an email cannot pick one).
    //   2) The code goes only to the account's CONFIRMED contact email.
    //   3) The code rules (6 digits, 2 minutes, 5 tries, 60 seconds between codes, hourly limits) are the ones in
    //      EmailCodeService, with the purpose PasswordReset.
    //   4) The request answer never depends on the account: an unknown name, an account without a confirmed email, and a
    //      refused send (waiting time, hourly limit, mail problem) all look the same to the caller. The reason is only logged.
    //   5) The new password only has to be filled in (Register has no other rule). It is checked BEFORE the code,
    //      so a missing password does not use up one of the 5 tries.
    //   6) After the password changed, every refresh token of the account is revoked: nobody can renew a session with
    //      the old password's login. (An access token that was already issued works until it expires, at most 15 minutes.)

    private const EmailCodePurpose Purpose = EmailCodePurpose.PasswordReset;

    private readonly IUserRepository _users;
    private readonly IContactEmailRepository _contactEmails;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IEmailCodeService _codes;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        IUserRepository users,
        IContactEmailRepository contactEmails,
        IRefreshTokenRepository refreshTokens,
        IEmailCodeService codes,
        ILogger<PasswordResetService> logger)
    {
        _users = users;
        _contactEmails = contactEmails;
        _refreshTokens = refreshTokens;
        _codes = codes;
        _logger = logger;
    }

    public async Task<PasswordResetRequestResultDto> RequestAsync(string? uniqueName)
    {
        var name = (uniqueName ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ArgumentException("Enter your unique name.");

        var result = new PasswordResetRequestResultDto
        {
            ExpiresInSeconds = _codes.CodeLifetimeSeconds,
            ResendInSeconds = _codes.ResendWaitSeconds
        };

        var user = await _users.GetByUniqueNameAsync(name);
        if (user is null) return result;

        var contact = await _contactEmails.GetAsync(user.Id);
        if (contact?.VerifiedAt is null) return result;

        try
        {
            await _codes.SendAsync(user.Id, user.Name, Purpose, contact.Email);
        }
        catch (ArgumentException ex)
        {
            // Wait time, hourly limit or a mail problem: not shown to the caller (rule 4), but visible in the console
            _logger.LogWarning("Password reset code for user {UserId} was not sent: {Reason}", user.Id, ex.Message);
        }

        return result;
    }

    public async Task ConfirmAsync(PasswordResetConfirmDto dto)
    {
        var name = (dto.UniqueName ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ArgumentException("Enter your unique name.");
        if (string.IsNullOrEmpty(dto.NewPassword))
            throw new ArgumentException("Enter a new password.");

        // An unknown name and an account without a confirmed email get the same answer as "no code was asked for"
        var user = await _users.GetByUniqueNameAsync(name);
        var contact = user is null ? null : await _contactEmails.GetAsync(user.Id);
        if (user is null || contact?.VerifiedAt is null)
            throw new ArgumentException("There is no active code. Request a new one.");

        // Throws a 400 with a message when the code is wrong, expired or out of tries. A right code is used up here.
        await _codes.VerifyAsync(user.Id, Purpose, contact.Email, dto.Code);

        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _users.UpdateAsync(user);

        await _refreshTokens.RevokeAllForUserAsync(user.Id);
    }
}
