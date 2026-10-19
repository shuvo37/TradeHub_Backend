using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Enums;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class EmailCodeRepository : IEmailCodeRepository
{
    private readonly TradeHubDbContext _context;

    public EmailCodeRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    public async Task<EmailCode?> GetAsync(Guid userId, EmailCodePurpose purpose)
    {
        return await _context.EmailCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Purpose == purpose);
    }

    public async Task SaveAsync(EmailCode code)
    {
        // Replacing an existing code is one atomic statement, no read first
        var updated = await UpdateExistingAsync(code);
        if (updated > 0) return;

        _context.EmailCodes.Add(code);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two requests of the same user created the row at the same moment: the other one won.
            // Forget our insert and put our code on the row that now exists.
            foreach (var entry in _context.ChangeTracker.Entries<EmailCode>().ToList())
                entry.State = EntityState.Detached;

            await UpdateExistingAsync(code);
        }
    }

    private Task<int> UpdateExistingAsync(EmailCode code)
    {
        return _context.EmailCodes
            .Where(c => c.UserId == code.UserId && c.Purpose == code.Purpose)
            .ExecuteUpdateAsync(c => c
                .SetProperty(x => x.Email, code.Email)
                .SetProperty(x => x.CodeHash, code.CodeHash)
                .SetProperty(x => x.CreatedAt, code.CreatedAt)
                .SetProperty(x => x.ExpiresAt, code.ExpiresAt)
                .SetProperty(x => x.Attempts, 0));
    }

    public async Task<EmailCode?> TryConsumeAttemptAsync(
        Guid userId, EmailCodePurpose purpose, DateTimeOffset now, int maxAttempts)
    {
        var counted = await _context.EmailCodes
            .Where(c => c.UserId == userId && c.Purpose == purpose && c.ExpiresAt > now && c.Attempts < maxAttempts)
            .ExecuteUpdateAsync(c => c.SetProperty(x => x.Attempts, x => x.Attempts + 1));

        if (counted == 0) return null;
        return await GetAsync(userId, purpose);
    }

    public async Task DeleteAsync(Guid userId, EmailCodePurpose purpose)
    {
        await _context.EmailCodes
            .Where(c => c.UserId == userId && c.Purpose == purpose)
            .ExecuteDeleteAsync();
    }

    public async Task AddSendAsync(EmailCodeSend send)
    {
        _context.EmailCodeSends.Add(send);
        await _context.SaveChangesAsync();
    }

    public async Task<DateTimeOffset?> GetLastSendAtAsync(Guid userId, EmailCodePurpose purpose)
    {
        return await _context.EmailCodeSends
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.Purpose == purpose)
            .OrderByDescending(s => s.SentAt)
            .Select(s => (DateTimeOffset?)s.SentAt)
            .FirstOrDefaultAsync();
    }

    public async Task<int> CountSendsByUserSinceAsync(Guid userId, EmailCodePurpose purpose, DateTimeOffset since)
    {
        return await _context.EmailCodeSends
            .CountAsync(s => s.UserId == userId && s.Purpose == purpose && s.SentAt > since);
    }

    public async Task<int> CountSendsToEmailSinceAsync(string email, EmailCodePurpose purpose, DateTimeOffset since)
    {
        return await _context.EmailCodeSends
            .CountAsync(s => s.Email == email && s.Purpose == purpose && s.SentAt > since);
    }

    public async Task DeleteOldSendsAsync(Guid userId, DateTimeOffset before)
    {
        await _context.EmailCodeSends
            .Where(s => s.UserId == userId && s.SentAt < before)
            .ExecuteDeleteAsync();
    }
}
