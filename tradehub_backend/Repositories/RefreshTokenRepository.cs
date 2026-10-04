using Microsoft.EntityFrameworkCore;
using TradeHub.Data;
using TradeHub.Models;

namespace TradeHub.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly TradeHubDbContext _db;

    public RefreshTokenRepository(TradeHubDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(RefreshToken token)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
    }

    public Task<RefreshToken?> GetByHashAsync(string hash) =>
        _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);

    public async Task UpdateAsync(RefreshToken token)
    {
        _db.RefreshTokens.Update(token);
        await _db.SaveChangesAsync();
    }

    // Used when a stolen token is detected: kills every active session of that user
    public Task RevokeAllForUserAsync(Guid userId) =>
        _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow));
}