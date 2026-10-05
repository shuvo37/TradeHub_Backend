using TradeHub.Models;

namespace TradeHub.Repositories;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid id);
    Task<Comment?> GetDetailsByIdAsync(Guid id);
    Task<List<Comment>> GetDetailsByPostIdAsync(Guid postId);
    Task<int> CountByPostIdAsync(Guid postId);
    Task<Dictionary<Guid, int>> CountByPostIdsAsync(List<Guid> postIds);
    Task<Comment> AddAsync(Comment comment);
    Task<bool> DeleteAsync(Guid id);
}