using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Enums;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class PostRepository : IPostRepository
{
    private readonly TradeHubDbContext _context;

    public PostRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    public async Task<Post?> GetByIdAsync(Guid id)
    {
        return await _context.Posts.FindAsync(id);
    }

    // Read-only, with author and attached product loaded (for building PostDto)
    public async Task<Post?> GetDetailsByIdAsync(Guid id)
    {
        return await _context.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Product)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // Read-only, all posts of one user, newest first
    public async Task<List<Post>> GetDetailsByUserIdAsync(Guid userId)
    {
        return await _context.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Product)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    // Read-only, one page of the news feed: my own posts and the posts of my accepted friends, newest first.
    // "Newest" means the feed time: RevivedAt when the post was revived, otherwise CreatedAt (becomes COALESCE in SQL).
    // 'before' is a feed time too (the NextCursor of the previous page).
    public async Task<List<Post>> GetFeedAsync(Guid userId, DateTimeOffset? before, int take)
    {
        // My friends' ids: in every accepted row that I am part of, the person who is not me.
        // This stays an IQueryable, so it becomes a sub-query inside the same SQL statement (one round trip).
        var friendIds = _context.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        (f.RequesterId == userId || f.AddresseeId == userId))
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId);

        var query = _context.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Product)
            .Where(p => p.UserId == userId || friendIds.Contains(p.UserId));

        if (before != null)
            query = query.Where(p => (p.RevivedAt ?? p.CreatedAt) < before.Value);

        return await query
            .OrderByDescending(p => p.RevivedAt ?? p.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Post> AddAsync(Post post)
    {
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task<bool> UpdateAsync(Post post)
    {
        var existing = await _context.Posts.FindAsync(post.Id);
        if (existing == null) return false;

        _context.Entry(existing).CurrentValues.SetValues(post);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.Posts.FindAsync(id);
        if (existing == null) return false;

        _context.Posts.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}