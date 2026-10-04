using TradeHub.Dtos.Posts;
using TradeHub.Dtos.Products;
using TradeHub.Models;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly IProductRepository _productRepository;

    public PostService(IPostRepository postRepository, IProductRepository productRepository)
    {
        _postRepository = postRepository;
        _productRepository = productRepository;
    }

    // Any logged-in user can read a post.
    public async Task<PostDto> GetAsync(Guid postId)
    {
        var post = await _postRepository.GetDetailsByIdAsync(postId);
        if (post == null)
            throw new KeyNotFoundException("Post not found");

        return ToDto(post);
    }

    // Any logged-in user can read a user's posts, newest first.
    public async Task<List<PostDto>> GetByUserAsync(Guid userId)
    {
        var posts = await _postRepository.GetDetailsByUserIdAsync(userId);
        return posts.Select(ToDto).ToList();
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

        // Reload with author and product so the dto is complete
        var saved = await _postRepository.GetDetailsByIdAsync(post.Id);
        return ToDto(saved!);
    }

    public async Task UpdateTextAsync(Guid userId, Guid postId, UpdatePostDto dto)
    {
        var post = await GetOwnedAsync(userId, postId);
        var text = (dto.Text ?? string.Empty).Trim();

        // Image and product can't change, so if the post has neither, the text can't be empty
        if (text.Length == 0 && string.IsNullOrEmpty(post.Image) && post.ProductId == null)
            throw new ArgumentException("A post needs text, an image, or a product");

        post.Text = text;

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

    private static PostDto ToDto(Post p) => new()
    {
        Id = p.Id,
        Text = p.Text,
        Image = p.Image ?? string.Empty,
        CreatedAt = p.CreatedAt,
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
        }
    };
}