using TradeHub.Dtos.Posts;
using TradeHub.Dtos.Products;
using TradeHub.Models;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILikeRepository _likeRepository;
    private readonly ICommentRepository _commentRepository;

    public PostService(
        IPostRepository postRepository,
        IProductRepository productRepository,
        ILikeRepository likeRepository,
        ICommentRepository commentRepository)
    {
        _postRepository = postRepository;
        _productRepository = productRepository;
        _likeRepository = likeRepository;
        _commentRepository = commentRepository;
    }

    // Any logged-in user can read a post. viewerId decides LikedByMe.
    public async Task<PostDto> GetAsync(Guid viewerId, Guid postId)
    {
        var post = await _postRepository.GetDetailsByIdAsync(postId);
        if (post == null)
            throw new KeyNotFoundException("Post not found");

        var dtos = await ToDtosAsync(new List<Post> { post }, viewerId);
        return dtos[0];
    }

    // Any logged-in user can read a user's posts, newest first. viewerId decides LikedByMe.
    public async Task<List<PostDto>> GetByUserAsync(Guid viewerId, Guid userId)
    {
        var posts = await _postRepository.GetDetailsByUserIdAsync(userId);
        return await ToDtosAsync(posts, viewerId);
    }

    // How many posts one feed page holds
    private const int FeedPageSize = 10;

    // The news feed of the logged-in user: their own posts and their accepted friends' posts, newest first, one page at a time.
    // A revived post counts from its revive time; a post never revived counts from its creation time.
    // We ask for one extra row: if it exists there is an older page, and it is not sent to the client.
    public async Task<FeedPageDto> GetFeedAsync(Guid userId, DateTimeOffset? before)
    {
        var rows = await _postRepository.GetFeedAsync(userId, before, FeedPageSize + 1);
        var posts = rows.Take(FeedPageSize).ToList();

        // The next page starts after the feed time of the last post of this page
        DateTimeOffset? nextCursor = null;
        if (posts.Count > 0)
            nextCursor = posts[^1].RevivedAt ?? posts[^1].CreatedAt;

        return new FeedPageDto
        {
            Items = await ToDtosAsync(posts, userId),
            HasMore = rows.Count > FeedPageSize,
            NextCursor = nextCursor
        };
    }

    public async Task<PostDto> CreateAsync(Guid userId, CreatePostDto dto)
    {
        var text = (dto.Text ?? string.Empty).Trim();
        var image = string.IsNullOrWhiteSpace(dto.Image) ? null : dto.Image.Trim();

        if (text.Length == 0 && image == null && dto.ProductId == null)
            throw new ArgumentException("A post needs text, an image, or a product");

        // The attached product must belong to the person posting
        if (dto.ProductId != null)
        {
            var product = await _productRepository.GetByIdAsync(dto.ProductId.Value);
            if (product == null || product.Category == null || product.Category.UserId != userId)
                throw new KeyNotFoundException("Product not found");
        }

        var post = new Post
        {
            Id = Guid.NewGuid(),
            Text = text,
            Image = image,
            ProductId = dto.ProductId,
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _postRepository.AddAsync(post);

        // Reload with author and product so the dto is complete.
        // A brand-new post has no likes and no comments, so the counts are 0 and LikedByMe is false.
        var saved = await _postRepository.GetDetailsByIdAsync(post.Id);
        return ToDto(saved!, 0, 0, false);
    }

    public async Task UpdateTextAsync(Guid userId, Guid postId, UpdatePostDto dto)
    {
        var post = await GetOwnedAsync(userId, postId);
        var text = (dto.Text ?? string.Empty).Trim();

        // Image and product can't change, so if the post has neither, the text can't be empty
        if (text.Length == 0 && string.IsNullOrEmpty(post.Image) && post.ProductId == null)
            throw new ArgumentException("A post needs text, an image, or a product");

        var textChanged = text != post.Text;
        post.Text = text;

        // An edit that really changes the text also revives the post, but only when the 24-hour wait is over.
        // Edits made during the wait are saved without touching RevivedAt, so three edits in one day revive once.
        var now = DateTimeOffset.UtcNow;
        if (textChanged && ReviveWait(post, now) <= TimeSpan.Zero)
            post.RevivedAt = now;

        if (!await _postRepository.UpdateAsync(post))
            throw new KeyNotFoundException("Post not found");
    }

    // The "Revive post" item in the post's menu: moves my post back to the top of the feed.
    // Only the author may do it (anyone else gets the same 404 as a missing post).
    // If the 24-hour wait is not over, the answer is 400 with the time left.
    public async Task ReviveAsync(Guid userId, Guid postId)
    {
        var post = await GetOwnedAsync(userId, postId);
        var now = DateTimeOffset.UtcNow;

        var wait = ReviveWait(post, now);
        if (wait > TimeSpan.Zero)
            throw new ArgumentException(ReviveWaitMessage(wait));

        post.RevivedAt = now; // replaces the previous revive time, if there was one

        if (!await _postRepository.UpdateAsync(post))
            throw new KeyNotFoundException("Post not found");
    }

    public async Task DeleteAsync(Guid userId, Guid postId)
    {
        await GetOwnedAsync(userId, postId);

        if (!await _postRepository.DeleteAsync(postId))
            throw new KeyNotFoundException("Post not found");
    }

    // Only the author may change or delete a post.
    // Anyone else gets the same 404 as a missing post.
    private async Task<Post> GetOwnedAsync(Guid userId, Guid postId)
    {
        var post = await _postRepository.GetByIdAsync(postId);

        if (post == null || post.UserId != userId)
            throw new KeyNotFoundException("Post not found");

        return post;
    }

    // A post can be revived once in 24 hours. Editing and the "Revive post" menu item share this one wait.
    private static readonly TimeSpan ReviveCooldown = TimeSpan.FromHours(24);

    // How long until this post may be revived again (zero or less = it may be revived now).
    // The wait starts at the last revive, or at the creation time if the post was never revived.
    private static TimeSpan ReviveWait(Post post, DateTimeOffset now)
        => (post.RevivedAt ?? post.CreatedAt) + ReviveCooldown - now;

    // "You can revive this post again in 5h 12m" (rounded up to the next minute)
    private static string ReviveWaitMessage(TimeSpan wait)
    {
        var totalMinutes = (int)Math.Ceiling(wait.TotalMinutes);
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        var left = hours == 0 ? $"{minutes}m"
                 : minutes == 0 ? $"{hours}h"
                 : $"{hours}h {minutes}m";

        return $"You can revive this post again in {left}";
    }

    // Builds the dtos for a whole list with 3 extra queries in total (not 3 per post).
    // The awaits run one after another on purpose: one DbContext can't run two queries at once.
    private async Task<List<PostDto>> ToDtosAsync(List<Post> posts, Guid viewerId)
    {
        var postIds = posts.Select(p => p.Id).ToList();

        var likeCounts = await _likeRepository.CountByPostIdsAsync(postIds);
        var commentCounts = await _commentRepository.CountByPostIdsAsync(postIds);
        var likedByViewer = await _likeRepository.GetLikedPostIdsAsync(viewerId, postIds);

        return posts
            .Select(p => ToDto(
                p,
                likeCounts.GetValueOrDefault(p.Id),     // missing = no likes = 0
                commentCounts.GetValueOrDefault(p.Id),  // missing = no comments = 0
                likedByViewer.Contains(p.Id)))
            .ToList();
    }

    private static PostDto ToDto(Post p, int likeCount, int commentCount, bool likedByMe) => new()
    {
        Id = p.Id,
        Text = p.Text,
        Image = p.Image ?? string.Empty,
        CreatedAt = p.CreatedAt,
        RevivedAt = p.RevivedAt,
        AuthorId = p.UserId,
        AuthorName = p.User?.Name ?? string.Empty,
        AuthorAvatar = p.User?.Avatar ?? string.Empty,
        Product = p.Product == null ? null : new ProductDto
        {
            Id = p.Product.Id,
            Name = p.Product.Name,
            Price = p.Product.Price ?? string.Empty,
            Description = p.Product.Description ?? string.Empty,
            Image = p.Product.Image ?? string.Empty,
            Quantity = p.Product.Quantity,
            Status = p.Product.Status,
            Discount = p.Product.Discount ?? 0
        },
        LikeCount = likeCount,
        CommentCount = commentCount,
        LikedByMe = likedByMe
    };
}