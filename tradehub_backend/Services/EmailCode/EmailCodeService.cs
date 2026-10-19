using System.Security.Cryptography;
using TradeHub.Enums;
using TradeHub.Models;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class EmailCodeService : IEmailCodeService
{
    // ---- The rules: change a number here and nothing else ----
    //
    //   1) The code has 6 digits and works for 2 minutes. Only a hash of it is stored.
    //   2) 5 tries per code (right or wrong, every try counts). After the 5th wrong one the code is dead.
    //   3) A new code can be asked only 60 seconds after the last one. A new code cancels the old one.
    //   4) At most 5 codes per hour for one account, and at most 5 codes per hour to one email address
    //      (the same address can sit on many accounts, so this stops many accounts from flooding one inbox).
    //   5) A code only works for the address it was sent to.

    private const int CodeLength = 6;
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(2);
    private const int MaxTries = 5;
    private static readonly TimeSpan ResendWait = TimeSpan.FromSeconds(60);
    private const int MaxPerHourPerAccount = 5;
    private const int MaxPerHourPerAddress = 5;
    private static readonly TimeSpan LimitWindow = TimeSpan.FromHours(1);
    private static readonly TimeSpan LogKeepTime = TimeSpan.FromHours(24); // send-log lines older than this are removed

    private readonly IEmailCodeRepository _repository;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailCodeService> _logger;

    public EmailCodeService(IEmailCodeRepository repository, IEmailSender emailSender, ILogger<EmailCodeService> logger)
    {
        _repository = repository;
        _emailSender = emailSender;
        _logger = logger;
    }

    public int CodeLifetimeSeconds => (int)CodeLifetime.TotalSeconds;
    public int ResendWaitSeconds => (int)ResendWait.TotalSeconds;

    public async Task SendAsync(Guid userId, string userName, EmailCodePurpose purpose, string email)
    {
        var now = DateTimeOffset.UtcNow;

        // The wait between two codes
        var last = await _repository.GetLastSendAtAsync(userId, purpose);
        if (last is not null)
        {
            var wait = last.Value + ResendWait - now;
            if (wait > TimeSpan.Zero)
                throw new ArgumentException($"Wait {Seconds(wait)} seconds before asking for a new code.");
        }

        // The hourly limits
        var since = now - LimitWindow;
        if (await _repository.CountSendsByUserSinceAsync(userId, purpose, since) >= MaxPerHourPerAccount)
            throw new ArgumentException("Too many codes were requested. Try again in an hour.");
        if (await _repository.CountSendsToEmailSinceAsync(email, purpose, since) >= MaxPerHourPerAddress)
            throw new ArgumentException("Too many codes were sent to this email. Try again in an hour.");

        // The send is logged BEFORE the mail goes out, so a broken mail server cannot be hit without limit either
        await _repository.DeleteOldSendsAsync(userId, now - LogKeepTime);
        await _repository.AddSendAsync(new EmailCodeSend
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Purpose = purpose,
            Email = email,
            SentAt = now
        });

        var code = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, CodeLength)).ToString($"D{CodeLength}");

        // The code must exist in the database before the mail arrives, the user may type it at once
        await _repository.SaveAsync(new EmailCode
        {
            UserId = userId,
            Purpose = purpose,
            Email = email,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
            CreatedAt = now,
            ExpiresAt = now + CodeLifetime,
            Attempts = 0
        });

        var (subject, body) = BuildMessage(purpose, userName, code);
        try
        {
            await _emailSender.SendAsync(email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not send the {Purpose} code mail to user {UserId}", purpose, userId);
            await _repository.DeleteAsync(userId, purpose);
            throw new ArgumentException("We couldn't send the email. Try again in a moment.");
        }
    }

    public async Task VerifyAsync(Guid userId, EmailCodePurpose purpose, string email, string? code)
    {
        // A text that is not 6 digits is not a try: nothing is counted
        var typed = (code ?? string.Empty).Trim();
        if (typed.Length != CodeLength || !typed.All(char.IsAsciiDigit))
            throw new ArgumentException($"Enter the {CodeLength}-digit code.");

        var now = DateTimeOffset.UtcNow;

        // The try is counted first, in one statement: parallel requests cannot get more than MaxTries comparisons
        var row = await _repository.TryConsumeAttemptAsync(userId, purpose, now, MaxTries);
        if (row is null)
        {
            var existing = await _repository.GetAsync(userId, purpose);
            if (existing is null)
                throw new ArgumentException("There is no active code. Request a new one.");
            if (existing.ExpiresAt <= now)
            {
                await _repository.DeleteAsync(userId, purpose);
                throw new ArgumentException("This code has expired. Request a new one.");
            }
            throw new ArgumentException("Too many wrong tries. Request a new code.");
        }

        // The code was sent to another address (the email was changed since)
        if (row.Email != email)
        {
            await _repository.DeleteAsync(userId, purpose);
            throw new ArgumentException("There is no active code. Request a new one.");
        }

        if (BCrypt.Net.BCrypt.Verify(typed, row.CodeHash))
        {
            await _repository.DeleteAsync(userId, purpose); // used up
            return;
        }

        var left = MaxTries - row.Attempts;
        if (left <= 0)
        {
            await _repository.DeleteAsync(userId, purpose);
            throw new ArgumentException("Wrong code. No tries left, request a new code.");
        }
        throw new ArgumentException($"Wrong code. {left} {(left == 1 ? "try" : "tries")} left.");
    }

    public async Task<EmailCodeStatus> GetStatusAsync(Guid userId, EmailCodePurpose purpose, string email)
    {
        var now = DateTimeOffset.UtcNow;

        var expiresIn = 0;
        var code = await _repository.GetAsync(userId, purpose);
        if (code is not null && code.Email == email && code.Attempts < MaxTries && code.ExpiresAt > now)
            expiresIn = Seconds(code.ExpiresAt - now);

        var resendIn = 0;
        var last = await _repository.GetLastSendAtAsync(userId, purpose);
        if (last is not null)
        {
            var wait = last.Value + ResendWait - now;
            if (wait > TimeSpan.Zero) resendIn = Seconds(wait);
        }

        return new EmailCodeStatus(expiresIn, resendIn);
    }

    // Whole seconds, rounded up, so "Wait 0 seconds" never shows
    private static int Seconds(TimeSpan span) => (int)Math.Ceiling(span.TotalSeconds);

    // The mail itself. A new purpose (password reset) adds a case here.
    private static (string Subject, string Body) BuildMessage(EmailCodePurpose purpose, string userName, string code)
    {
        var minutes = (int)CodeLifetime.TotalMinutes;
        switch (purpose)
        {
            case EmailCodePurpose.PasswordReset:
                return (
                    "Password reset code from TradeHub",
                    $"Hi {userName}, your password reset code is: {code}\n\n" +
                    $"It works for {minutes} minutes. If you did not ask to reset your password, you can ignore this email: " +
                    "your password has not been changed.");

            default:
                return (
                    "Confirmation code from TradeHub",
                    $"Hi {userName}, your confirmation code is: {code}\n\n" +
                    $"It works for {minutes} minutes. If you did not ask for it, you can ignore this email.");
        }
    }
}
