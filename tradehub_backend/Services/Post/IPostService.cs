using TradeHub.Dtos.Posts;

namespace TradeHub.Services;

public interface IPostService
{
    Task<PostDto> GetAsync(Guid postId);
    Task<List<PostDto>> GetByUserAsync(Guid userId);
    Task<PostDto> CreateAsync(Guid userId, CreatePostDto dto);
    Task UpdateTextAsync(Guid userId, Guid postId, UpdatePostDto dto);
    Task DeleteAsync(Guid userId, Guid postId);
}