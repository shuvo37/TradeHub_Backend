using TradeHub.Models;

namespace TradeHub.Repositories;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(Guid id);
    Task<Post?> GetDetailsByIdAsync(Guid id);
    Task<List<Post>> GetDetailsByUserIdAsync(Guid userId);
    Task<Post> AddAsync(Post post);
    Task<bool> UpdateAsync(Post post);
    Task<bool> DeleteAsync(Guid id);
}