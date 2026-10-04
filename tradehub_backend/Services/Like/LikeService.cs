using TradeHub.Models;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class LikeService : ILikeService
{
    private readonly ILikeRepository _likeRepository;
    private readonly IPostRepository _postRepository;

    public LikeService(ILikeRepository likeRepository, IPostRepository postRepository)
    {
        _likeRepository = likeRepository;
        _postRepository = postRepository;
    }

    public async Task LikeAsync(Guid userId, Guid postId)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            throw new KeyNotFoundException("Post not found");

        // false means already liked. That is fine, so the result is ignored on purpose.
        await _likeRepository.AddAsync(new Like { PostId = postId, UserId = userId });
    }

    public async Task UnlikeAsync(Guid userId, Guid postId)
    {
        // false means there was no like. Also fine, so the result is ignored on purpose.
        await _likeRepository.DeleteAsync(postId, userId);
    }

    // Any logged-in user can read the like count of a post.
    public async Task<int> CountAsync(Guid postId)
    {
        return await _likeRepository.CountByPostIdAsync(postId);
    }
}