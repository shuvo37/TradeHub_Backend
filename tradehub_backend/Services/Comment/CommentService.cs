using TradeHub.Dtos.Comments;
using TradeHub.Models;
using TradeHub.Repositories;
namespace TradeHub.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IPostRepository _postRepository;

    public CommentService(ICommentRepository commentRepository, IPostRepository postRepository)
    {
        _commentRepository = commentRepository;
        _postRepository = postRepository;
    }

    // Any logged-in user can read a comment.
    public async Task<CommentDto> GetAsync(Guid commentId)
    {
        var comment = await _commentRepository.GetDetailsByIdAsync(commentId);
        if (comment == null)
            throw new KeyNotFoundException("Comment not found");

        return ToDto(comment);
    }

    // Any logged-in user can read a post's comments, oldest first.
    public async Task<List<CommentDto>> GetByPostAsync(Guid postId)
    {
        var comments = await _commentRepository.GetDetailsByPostIdAsync(postId);
        return comments.Select(ToDto).ToList();
    }

    // Any logged-in user can read the comment count of a post.
    public async Task<int> CountByPostAsync(Guid postId)
    {
        return await _commentRepository.CountByPostIdAsync(postId);
    }

    public async Task<CommentDto> CreateAsync(Guid userId, Guid postId, CreateCommentDto dto)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            throw new KeyNotFoundException("Post not found");

        var text = (dto.Text ?? string.Empty).Trim();
        if (text.Length == 0)
            throw new ArgumentException("Comment text is required");

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Text = text,
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _commentRepository.AddAsync(comment);

        // Reload with the author so the dto has the name and avatar
        var saved = await _commentRepository.GetDetailsByIdAsync(comment.Id);
        return ToDto(saved!);
    }

    public async Task DeleteAsync(Guid userId, Guid commentId)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);

        if (comment == null || comment.Post == null)
            throw new KeyNotFoundException("Comment not found");

        // The comment's author or the post's owner may delete it.
        // Anyone else gets the same 404 as a missing comment.
        var isAuthor = comment.UserId == userId;
        var isPostOwner = comment.Post.UserId == userId;

        if (!isAuthor && !isPostOwner)
            throw new KeyNotFoundException("Comment not found");

        if (!await _commentRepository.DeleteAsync(commentId))
            throw new KeyNotFoundException("Comment not found");
    }

    private static CommentDto ToDto(Comment c) => new()
    {
        Id = c.Id,
        PostId = c.PostId,
        Text = c.Text,
        CreatedAt = c.CreatedAt,
        AuthorId = c.UserId,
        AuthorName = c.User?.Name ?? string.Empty,
        AuthorAvatar = c.User?.Avatar ?? string.Empty
    };
}