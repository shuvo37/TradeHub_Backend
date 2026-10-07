using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class UserRepository : IUserRepository
{
    private readonly TradeHubDbContext _context;

    public UserRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> UpdateAsync(User user)
    {
        var existing = await _context.Users.FindAsync(user.Id);
        if (existing is null) return false;

        _context.Entry(existing).CurrentValues.SetValues(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Users.FindAsync(id);
        if (existing is null) return false;

        _context.Users.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<User?>GetByUniqueNameAsync(string uniqueName)
    {

          return await _context.Users.FirstOrDefaultAsync(u => u.UniqueName == uniqueName);

    }

    // Search by text: (1) the account whose unique name is exactly the text, (2) then accounts whose
    // name contains the text. Both ignore upper/lower case. `excludeUserId` is the person searching.
    public async Task<List<User>> SearchAsync(string text, Guid excludeUserId, int take)
    {
        var escaped = EscapeLike(text);

        // 1. Exact unique name (no % in the pattern, so ILIKE acts as a case-insensitive "equals")
        var exact = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id != excludeUserId && EF.Functions.ILike(u.UniqueName, escaped, "\\"))
            .ToListAsync();

        var exactIds = exact.Select(u => u.Id).ToList();

        // 2. Name contains the text, alphabetical, without repeating the exact matches
        var byName = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id != excludeUserId
                        && !exactIds.Contains(u.Id)
                        && EF.Functions.ILike(u.Name, "%" + escaped + "%", "\\"))
            .OrderBy(u => u.Name)
            .ThenBy(u => u.UniqueName)
            .Take(take)
            .ToListAsync();

        return exact.Concat(byName).Take(take).ToList();
    }

    // In LIKE patterns % and _ are wildcards, and unique names contain "_".
    // Putting the escape character "\" in front makes them match themselves.
    private static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
   


}