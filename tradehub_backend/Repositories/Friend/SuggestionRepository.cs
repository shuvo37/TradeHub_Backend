using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Enums;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class SuggestionRepository : ISuggestionRepository
{
    private readonly TradeHubDbContext _context;

    public SuggestionRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> GetExcludedIdsAsync(Guid userId)
    {
        // Every friendship row I am part of, pending or accepted, whoever sent it
        var rows = await _context.Friendships
            .AsNoTracking()
            .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
            .Select(f => new { f.RequesterId, f.AddresseeId })
            .ToListAsync();

        var ids = rows
            .Select(r => r.RequesterId == userId ? r.AddresseeId : r.RequesterId)
            .ToList();
        ids.Add(userId);
        return ids;
    }

    public async Task<List<SuggestionSignals>> GetSignalsAsync(
        Guid userId, List<Guid> excluded, DateTimeOffset since, int topSellers)
    {
        var skip = new HashSet<Guid>(excluded);
        var signals = new Dictionary<Guid, SuggestionSignals>();

        // The row of one possible suggestion (created on first use). Excluded people get no row.
        SuggestionSignals? Slot(Guid id)
        {
            if (skip.Contains(id)) return null;
            if (!signals.TryGetValue(id, out var s))
            {
                s = new SuggestionSignals { UserId = id };
                signals[id] = s;
            }
            return s;
        }

        // 1) Mutual friends: my accepted friends, then the people THEY are friends with
        var friendIds = await _context.Friendships
            .AsNoTracking()
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        (f.RequesterId == userId || f.AddresseeId == userId))
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
            .ToListAsync();

        if (friendIds.Count > 0)
        {
            // A friend of mine who SENT the request: the other person is the addressee
            var viaRequester = await _context.Friendships
                .AsNoTracking()
                .Where(f => f.Status == FriendshipStatus.Accepted && friendIds.Contains(f.RequesterId))
                .GroupBy(f => f.AddresseeId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            // A friend of mine who RECEIVED the request: the other person is the requester
            var viaAddressee = await _context.Friendships
                .AsNoTracking()
                .Where(f => f.Status == FriendshipStatus.Accepted && friendIds.Contains(f.AddresseeId))
                .GroupBy(f => f.RequesterId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var row in viaRequester)
            {
                var s = Slot(row.Id);
                if (s != null) s.MutualFriends += row.Count;
            }
            foreach (var row in viaAddressee)
            {
                var s = Slot(row.Id);
                if (s != null) s.MutualFriends += row.Count;
            }
        }

        // 2) Orders between us, either direction (rejected ones do not count)
        var orderPairs = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderStatus != OrderStatus.REJECTED &&
                        (o.BuyerId == userId || o.SellerId == userId))
            .GroupBy(o => new { o.BuyerId, o.SellerId })
            .Select(g => new { g.Key.BuyerId, g.Key.SellerId, Count = g.Count() })
            .ToListAsync();

        foreach (var pair in orderPairs)
        {
            if (pair.BuyerId == userId)
            {
                var s = Slot(pair.SellerId);
                if (s != null) s.OrdersIBought += pair.Count;
            }
            else
            {
                var s = Slot(pair.BuyerId);
                if (s != null) s.OrdersTheyBought += pair.Count;
            }
        }

        // 3) Likes and comments between us, either direction.
        // A like has no date, so likes count for all time; comments only count from 'since'.
        var likesToMe = await _context.Likes
            .AsNoTracking()
            .Where(l => l.Post!.UserId == userId && l.UserId != userId)
            .GroupBy(l => l.UserId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync();

        var likesFromMe = await _context.Likes
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Post!.UserId != userId)
            .GroupBy(l => l.Post!.UserId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync();

        var commentsToMe = await _context.Comments
            .AsNoTracking()
            .Where(c => c.Post!.UserId == userId && c.UserId != userId && c.CreatedAt >= since)
            .GroupBy(c => c.UserId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync();

        var commentsFromMe = await _context.Comments
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Post!.UserId != userId && c.CreatedAt >= since)
            .GroupBy(c => c.Post!.UserId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var row in likesToMe)
        {
            var s = Slot(row.Id);
            if (s != null) s.LikesToMe += row.Count;
        }
        foreach (var row in likesFromMe)
        {
            var s = Slot(row.Id);
            if (s != null) s.LikesFromMe += row.Count;
        }
        foreach (var row in commentsToMe)
        {
            var s = Slot(row.Id);
            if (s != null) s.CommentsToMe += row.Count;
        }
        foreach (var row in commentsFromMe)
        {
            var s = Slot(row.Id);
            if (s != null) s.CommentsFromMe += row.Count;
        }

        // 4) The top sellers by recent order requests join the pool even if we share nothing
        var topSellerIds = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= since && !excluded.Contains(o.SellerId))
            .GroupBy(o => o.SellerId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Id)
            .Take(topSellers)
            .Select(x => x.Id)
            .ToListAsync();

        foreach (var id in topSellerIds)
            Slot(id);

        // 5) Order requests received as a seller, for everyone in the pool, in ONE query
        var poolIds = signals.Keys.ToList();
        if (poolIds.Count > 0)
        {
            var sellerOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= since && poolIds.Contains(o.SellerId))
                .GroupBy(o => o.SellerId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var row in sellerOrders)
            {
                if (signals.TryGetValue(row.Id, out var s)) s.SellerOrders = row.Count;
            }
        }

        return signals.Values.ToList();
    }

    public async Task<List<Guid>> GetActiveSellerIdsAsync(List<Guid> excluded, int take)
    {
        // A seller owns categories, and categories own products
        return await _context.Products
            .AsNoTracking()
            .Where(p => !excluded.Contains(p.Category!.UserId))
            .GroupBy(p => p.Category!.UserId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Id)
            .Take(take)
            .Select(x => x.Id)
            .ToListAsync();
    }

    public async Task<List<User>> GetUsersAsync(List<Guid> ids)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync();
    }
}
