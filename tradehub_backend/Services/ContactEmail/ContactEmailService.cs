using System.Text.RegularExpressions;
using TradeHub.Dtos.ContactEmail;
using TradeHub.Enums;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class ContactEmailService : IContactEmailService
{
    private const int MaxEmailLength = 254; // the usual limit for an email address
    private const EmailCodePurpose Purpose = EmailCodePurpose.ContactEmailVerification;

    // Shape check: something@something.something with no spaces.
    // After it, a DNS lookup checks that the domain can receive mail (so "x@gmial.xyzabc" is refused).
    // Whether the mailbox itself exists is proven by the code sent to it, not here.
    private static readonly Regex EmailShape = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private readonly IContactEmailRepository _repository;
    private readonly IEmailCodeService _codes;
    private readonly IEmailDomainChecker _domainChecker;
    private readonly IUserRepository _users;

    public ContactEmailService(
        IContactEmailRepository repository,
        IEmailCodeService codes,
        IEmailDomainChecker domainChecker,
        IUserRepository users)
    {
        _repository = repository;
        _codes = codes;
        _domainChecker = domainChecker;
        _users = users;
    }

    public async Task<ContactEmailDto> GetAsync(Guid userId)
    {
        return await BuildDtoAsync(userId);
    }

    public async Task<ContactEmailDto> SaveAsync(Guid userId, SaveContactEmailDto dto)
    {
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (email.Length == 0)
            throw new ArgumentException("Enter your email.");
        if (email.Length > MaxEmailLength)
            throw new ArgumentException("This email is too long.");
        if (!EmailShape.IsMatch(email))
            throw new ArgumentException("Enter a valid email address.");

        var current = await _repository.GetAsync(userId);
        var sameEmail = current is not null && current.Email == email;

        // Already confirmed: nothing to save and no mail to send
        if (sameEmail && current!.VerifiedAt is not null)
            return await BuildDtoAsync(userId);

        // A new address must belong to a domain that can receive mail
        if (!sameEmail)
        {
            var domain = email[(email.LastIndexOf('@') + 1)..];
            if (!await _domainChecker.CanReceiveMailAsync(domain))
                throw new ArgumentException("This email domain can't receive mail. Check the spelling.");

            await _repository.SaveAsync(userId, email);
        }

        // Saved but not confirmed: send the code. If it cannot be sent (a limit, a mail problem) the email stays
        // saved, and the app shows the code step with a "Send code" button next time.
        await SendCodeAsync(userId, email);
        return await BuildDtoAsync(userId);
    }

    public async Task<ContactEmailDto> ResendAsync(Guid userId)
    {
        var current = await _repository.GetAsync(userId);
        if (current is null)
            throw new ArgumentException("Enter your email first.");
        if (current.VerifiedAt is not null)
            throw new ArgumentException("Your email is already verified.");

        await SendCodeAsync(userId, current.Email);
        return await BuildDtoAsync(userId);
    }

    public async Task VerifyAsync(Guid userId, VerifyContactEmailDto dto)
    {
        var current = await _repository.GetAsync(userId);
        if (current is null)
            throw new ArgumentException("Enter your email first.");
        if (current.VerifiedAt is not null)
            return; // already confirmed (for example a second tab)

        // Throws a 400 with a message when the code is wrong, expired or out of tries
        await _codes.VerifyAsync(userId, Purpose, current.Email, dto.Code);

        if (!await _repository.MarkVerifiedAsync(userId, current.Email, DateTimeOffset.UtcNow))
            throw new ArgumentException("Your email was changed. Request a new code.");
    }

    private async Task SendCodeAsync(Guid userId, string email)
    {
        var user = await _users.GetByIdAsync(userId) ?? throw new KeyNotFoundException("User not found.");
        await _codes.SendAsync(userId, user.Name, Purpose, email);
    }

    private async Task<ContactEmailDto> BuildDtoAsync(Guid userId)
    {
        var row = await _repository.GetAsync(userId);
        if (row is null) return new ContactEmailDto();

        if (row.VerifiedAt is not null)
            return new ContactEmailDto { Email = row.Email, Verified = true };

        var status = await _codes.GetStatusAsync(userId, Purpose, row.Email);
        return new ContactEmailDto
        {
            Email = row.Email,
            Verified = false,
            ExpiresInSeconds = status.ExpiresInSeconds,
            ResendInSeconds = status.ResendInSeconds
        };
    }
}
