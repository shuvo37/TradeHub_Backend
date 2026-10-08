using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Enums;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly TradeHubDbContext _context;

    public NotificationRepository(TradeHubDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> AddAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<List<Notification>> GetForUserAsync(Guid userId, int take)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Include(n => n.Actor)
            .Where(n => n.RecipientId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountUnreadAsync(Guid userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.RecipientId == userId && !n.IsRead);
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        await _context.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task DeleteByFriendshipAsync(Guid friendshipId, NotificationType type)
    {
        await _context.Notifications
            .Where(n => n.FriendshipId == friendshipId && n.Type == type)
            .ExecuteDeleteAsync();
    }

    public async Task DeleteByCommentAsync(Guid commentId)
    {
        await _context.Notifications
            .Where(n => n.CommentId == commentId)
            .ExecuteDeleteAsync();
    }
}
