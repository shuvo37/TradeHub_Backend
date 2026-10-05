using TradeHub.Dtos.Comments;

namespace TradeHub.Services;

public interface ICommentService
{
    Task<CommentDto> GetAsync(Guid commentId);
    Task<List<CommentDto>> GetByPostAsync(Guid postId);
    Task<int> CountByPostAsync(Guid postId);
    Task<CommentDto> CreateAsync(Guid userId, Guid postId, CreateCommentDto dto);
    Task UpdateAsync(Guid userId, Guid commentId, UpdateCommentDto dto);
    Task DeleteAsync(Guid userId, Guid commentId);
}
