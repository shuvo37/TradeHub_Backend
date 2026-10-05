using Microsoft.EntityFrameworkCore;
using Npgsql;
using TradeHub.Data;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class LikeRepository : ILikeRepository
{
    private readonly TradeHubDbContext _context;

    public LikeRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    // true = like added, false = this user already liked the post
    public async Task<bool> AddAsync(Like like)
    {
        _context.Likes.Add(like);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The failed row is still tracked by the context, so drop it
            _context.Entry(like).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid postId, Guid userId)
    {
        // Key order matches HasKey: PostId first, then UserId
        var existing = await _context.Likes.FindAsync(postId, userId);
        if (existing == null) return false;

        _context.Likes.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountByPostIdAsync(Guid postId)
    {
        return await _context.Likes.CountAsync(l => l.PostId == postId);
    }

    // Like counts for many posts in ONE query.
    // A post with no likes is simply missing from the result (the caller treats missing as 0).
    public async Task<Dictionary<Guid, int>> CountByPostIdsAsync(List<Guid> postIds)
    {
        return await _context.Likes
            .Where(l => postIds.Contains(l.PostId))
            .GroupBy(l => l.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);
    }

    // Which of these posts did this user like (ONE query)
    public async Task<HashSet<Guid>> GetLikedPostIdsAsync(Guid userId, List<Guid> postIds)
    {
        var likedIds = await _context.Likes
            .Where(l => l.UserId == userId && postIds.Contains(l.PostId))
            .Select(l => l.PostId)
            .ToListAsync();

        return likedIds.ToHashSet();
    }
}