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
   


}