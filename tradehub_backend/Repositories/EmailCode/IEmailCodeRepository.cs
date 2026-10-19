using TradeHub.Enums;
using TradeHub.Models;

namespace TradeHub.Repositories;

public interface IEmailCodeRepository
{
    // The active code of this user for this purpose, or null
    Task<EmailCode?> GetAsync(Guid userId, EmailCodePurpose purpose);
    // Creates the active code, or replaces the existing one (attempts go back to 0)
    Task SaveAsync(EmailCode code);
    // Counts one try, in one atomic statement: only if the code exists, has not expired and has tries left.
    // Returns the code row (with the hash to compare) or null when no try was allowed.
    Task<EmailCode?> TryConsumeAttemptAsync(Guid userId, EmailCodePurpose purpose, DateTimeOffset now, int maxAttempts);
    Task DeleteAsync(Guid userId, EmailCodePurpose purpose);

    // The send log (for the limits)
    Task AddSendAsync(EmailCodeSend send);
    Task<DateTimeOffset?> GetLastSendAtAsync(Guid userId, EmailCodePurpose purpose);
    Task<int> CountSendsByUserSinceAsync(Guid userId, EmailCodePurpose purpose, DateTimeOffset since);
    Task<int> CountSendsToEmailSinceAsync(string email, EmailCodePurpose purpose, DateTimeOffset since);
    // Removes this user's log lines older than the given time
    Task DeleteOldSendsAsync(Guid userId, DateTimeOffset before);
}
