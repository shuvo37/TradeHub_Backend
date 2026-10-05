using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly TradeHubDbContext _context;

    public CommentRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    // Loads the post too, so the service can see who owns the post
    public async Task<Comment?> GetByIdAsync(Guid id)
    {
        return await _context.Comments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    // Read-only, with the author loaded (for building CommentDto)
    public async Task<Comment?> GetDetailsByIdAsync(Guid id)
    {
        return await _context.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    // Read-only, all comments of one post, oldest first
    public async Task<List<Comment>> GetDetailsByPostIdAsync(Guid postId)
    {
        return await _context.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> CountByPostIdAsync(Guid postId)
    {
        return await _context.Comments.CountAsync(c => c.PostId == postId);
    }

    // Comment counts for many posts in ONE query.
    // A post with no comments is simply missing from the result (the caller treats missing as 0).
    public async Task<Dictionary<Guid, int>> CountByPostIdsAsync(List<Guid> postIds)
    {
        return await _context.Comments
            .Where(c => postIds.Contains(c.PostId))
            .GroupBy(c => c.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);
    }

    public async Task<Comment> AddAsync(Comment comment)
    {
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
        return comment;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Comments.FindAsync(id);
        if (existing == null) return false;

        _context.Comments.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}