using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Enums;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class DiscoverRepository : IDiscoverRepository
{
    private readonly TradeHubDbContext _context;

    public DiscoverRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> GetFriendIdsAsync(Guid userId)
    {
        return await _context.Friendships
            .AsNoTracking()
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        (f.RequesterId == userId || f.AddresseeId == userId))
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
            .ToListAsync();
    }

    public async Task<List<Post>> GetCandidatePostsAsync(
        Guid userId, List<Guid> friendIds, DateTimeOffset since, int take)
    {
        return await _context.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Product).ThenInclude(pr => pr!.Category)
            .Where(p => p.UserId != userId &&
                        !friendIds.Contains(p.UserId) &&
                        (p.RevivedAt ?? p.CreatedAt) >= since)
            .OrderByDescending(p => p.RevivedAt ?? p.CreatedAt)
            .ThenBy(p => p.Id)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, int>> GetMutualFriendCountsAsync(List<Guid> friendIds, List<Guid> authorIds)
    {
        var result = new Dictionary<Guid, int>();
        if (friendIds.Count == 0 || authorIds.Count == 0) return result;

        // A friend of mine who SENT the request to the author
        var viaRequester = await _context.Friendships
            .AsNoTracking()
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        friendIds.Contains(f.RequesterId) &&
                        authorIds.Contains(f.AddresseeId))
            .GroupBy(f => f.AddresseeId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync();

        // A friend of mine who RECEIVED the request from the author
        var viaAddressee = await _context.Friendships
            .AsNoTracking()
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        friendIds.Contains(f.AddresseeId) &&
                        authorIds.Contains(f.RequesterId))
            .GroupBy(f => f.RequesterId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var row in viaRequester)
            result[row.Id] = result.GetValueOrDefault(row.Id) + row.Count;
        foreach (var row in viaAddressee)
            result[row.Id] = result.GetValueOrDefault(row.Id) + row.Count;

        return result;
    }

    public async Task<HashSet<Guid>> GetTradePartnerIdsAsync(Guid userId, List<Guid> authorIds)
    {
        var partners = new HashSet<Guid>();
        if (authorIds.Count == 0) return partners;

        // Authors I ordered from
        var soldToMe = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderStatus != OrderStatus.REJECTED &&
                        o.BuyerId == userId &&
                        authorIds.Contains(o.SellerId))
            .Select(o => o.SellerId)
            .Distinct()
            .ToListAsync();

        // Authors who ordered from me
        var boughtFromMe = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderStatus != OrderStatus.REJECTED &&
                        o.SellerId == userId &&
                        authorIds.Contains(o.BuyerId))
            .Select(o => o.BuyerId)
            .Distinct()
            .ToListAsync();

        partners.UnionWith(soldToMe);
        partners.UnionWith(boughtFromMe);
        return partners;
    }

    public async Task<Dictionary<Guid, int>> GetSellerOrderCountsAsync(List<Guid> authorIds, DateTimeOffset since)
    {
        var result = new Dictionary<Guid, int>();
        if (authorIds.Count == 0) return result;

        var rows = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= since && authorIds.Contains(o.SellerId))
            .GroupBy(o => o.SellerId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var row in rows)
            result[row.Id] = row.Count;

        return result;
    }

    public async Task<HashSet<string>> GetInterestCategoriesAsync(Guid userId)
    {
        // Categories I ordered from (the order keeps its own copy of the category name)
        var ordered = await _context.Orders
            .AsNoTracking()
            .Where(o => o.BuyerId == userId && o.CategoryName != "")
            .Select(o => o.CategoryName)
            .Distinct()
            .ToListAsync();

        // Categories of the products attached to posts I liked
        var liked = await _context.Likes
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Post!.Product != null)
            .Select(l => l.Post!.Product!.Category!.Name)
            .Distinct()
            .ToListAsync();

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in ordered.Concat(liked))
        {
            var clean = (name ?? string.Empty).Trim();
            if (clean.Length > 0) names.Add(clean);
        }
        return names;
    }
}
