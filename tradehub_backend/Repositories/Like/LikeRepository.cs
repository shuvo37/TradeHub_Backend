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
}