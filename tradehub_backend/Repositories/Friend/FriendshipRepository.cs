using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Enums;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class FriendshipRepository : IFriendshipRepository
{
    private readonly TradeHubDbContext _context;

    public FriendshipRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    public async Task<Friendship?> GetByIdAsync(Guid id)
    {
        return await _context.Friendships.FindAsync(id);
    }

    public async Task<Friendship?> GetBetweenAsync(Guid userA, Guid userB)
    {
        return await _context.Friendships.FirstOrDefaultAsync(f =>
            (f.RequesterId == userA && f.AddresseeId == userB) ||
            (f.RequesterId == userB && f.AddresseeId == userA));
    }

    public async Task<List<Friendship>> GetBetweenManyAsync(Guid userId, List<Guid> otherIds)
    {
        return await _context.Friendships
            .AsNoTracking()
            .Where(f =>
                (f.RequesterId == userId && otherIds.Contains(f.AddresseeId)) ||
                (f.AddresseeId == userId && otherIds.Contains(f.RequesterId)))
            .ToListAsync();
    }

    public async Task<List<Friendship>> GetReceivedPendingAsync(Guid userId)
    {
        return await _context.Friendships
            .AsNoTracking()
            .Include(f => f.Requester)
            .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    // Accepted rows that I am part of (as Requester or Addressee), newest first.
    // Both people are loaded; the service picks the one who is not me.
    public async Task<List<Friendship>> GetAcceptedAsync(Guid userId, DateTimeOffset? before, int take)
    {
        var query = _context.Friendships
            .AsNoTracking()
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        (f.RequesterId == userId || f.AddresseeId == userId));

        if (before != null)
            query = query.Where(f => f.CreatedAt < before.Value);

        return await query
            .OrderByDescending(f => f.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountUnseenReceivedAsync(Guid userId)
    {
        return await _context.Friendships
            .CountAsync(f => f.AddresseeId == userId
                             && f.Status == FriendshipStatus.Pending
                             && !f.SeenByAddressee);
    }

    public async Task MarkReceivedSeenAsync(Guid userId)
    {
        await _context.Friendships
            .Where(f => f.AddresseeId == userId
                        && f.Status == FriendshipStatus.Pending
                        && !f.SeenByAddressee)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.SeenByAddressee, true));
    }

    public async Task<Friendship> AddAsync(Friendship friendship)
    {
        _context.Friendships.Add(friendship);
        await _context.SaveChangesAsync();
        return friendship;
    }

    public async Task<bool> UpdateAsync(Friendship friendship)
    {
        var existing = await _context.Friendships.FindAsync(friendship.Id);
        if (existing == null) return false;

        _context.Entry(existing).CurrentValues.SetValues(friendship);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Friendships.FindAsync(id);
        if (existing == null) return false;

        _context.Friendships.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}
