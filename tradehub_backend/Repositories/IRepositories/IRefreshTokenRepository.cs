using TradeHub.Models;

namespace TradeHub.Repositories;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> GetByHashAsync(string hash);
    Task UpdateAsync(RefreshToken token);
    Task RevokeAllForUserAsync(Guid userId);
}