using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class ContactEmailRepository : IContactEmailRepository
{
    private readonly TradeHubDbContext _context;

    public ContactEmailRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    public async Task<UserContactEmail?> GetAsync(Guid userId)
    {
        return await _context.ContactEmails
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task SaveAsync(Guid userId, string email)
    {
        // A different email on an existing row: one atomic statement, no read first. A new address is not verified.
        var changed = await UpdateExistingAsync(userId, email);
        if (changed > 0) return;

        // No row changed: either the same email is already there (nothing to do) or there is no row yet
        if (await _context.ContactEmails.AnyAsync(c => c.UserId == userId)) return;

        _context.ContactEmails.Add(new UserContactEmail { UserId = userId, Email = email });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The same user saved at the same moment (two tabs, a double click): the other request inserted the row
            // first. Forget our insert and put the email on the row that now exists.
            foreach (var entry in _context.ChangeTracker.Entries<UserContactEmail>().ToList())
                entry.State = EntityState.Detached;

            await UpdateExistingAsync(userId, email);
        }
    }

    private Task<int> UpdateExistingAsync(Guid userId, string email)
    {
        return _context.ContactEmails
            .Where(c => c.UserId == userId && c.Email != email)
            .ExecuteUpdateAsync(c => c
                .SetProperty(x => x.Email, email)
                .SetProperty(x => x.VerifiedAt, (DateTimeOffset?)null));
    }

    public async Task<bool> MarkVerifiedAsync(Guid userId, string email, DateTimeOffset now)
    {
        var updated = await _context.ContactEmails
            .Where(c => c.UserId == userId && c.Email == email)
            .ExecuteUpdateAsync(c => c.SetProperty(x => x.VerifiedAt, (DateTimeOffset?)now));

        return updated > 0;
    }
}
