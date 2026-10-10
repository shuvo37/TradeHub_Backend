using TradeHub.Dtos.Posts;
using TradeHub.Dtos.Products;
using TradeHub.Models;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class DiscoverService : IDiscoverService
{
    // ---- The ranking policy for Discover "For you": change a number here and nothing else ----
    //
    // Which posts are looked at: posts of the last 7 days (a revived post counts from its revive time)
    // that are not mine and not my friends' (those are already in the news feed).
    //
    // A post's points (the "raw" score) are the sum of five parts:
    //   1) Mutual friends with the author: 6 points per mutual friend, up to 5 friends (30 at most)
    //   2) Trade with the author: 15 points if we have an order together, either direction (rejected orders do not count)
    //   3) Interest: 20 points if the product attached to the post is in a category I ordered from
    //      or in a category of a product on a post I liked (names compared without upper/lower case)
    //   4) Seller popularity: 1 point per order request the author received in the last 90 days, up to 15
    //   5) Engagement: 1 point per like and 3 per comment, up to 30
    // The raw score is then multiplied by a freshness factor that halves every 48 hours.
    // Posts with no points at all still stay in the list; they come last, newest first.

    private const int PointsPerMutualFriend = 6;
    private const int MaxMutualFriends = 5;          // 30 points at most

    private const int PointsTrade = 15;

    private const int PointsInterest = 20;

    private const int PointsPerSellerOrder = 1;
    private const int MaxSellerPoints = 15;

    private const int PointsPerLike = 1;
    private const int PointsPerComment = 3;
    private const int MaxEngagementPoints = 30;

    private const double HalfLifeHours = 48;         // the score halves every 48 hours of age

    private const int RecentDays = 7;                // only posts this young are looked at
    private const int SellerDays = 90;               // window for the seller's order requests
    private const int CandidateLimit = 300;          // at most this many (newest) posts are scored
    private const int PoolSize = 50;                 // the ranked list holds at most this many posts
    private const int PageSize = 10;                 // posts per page

    private readonly IDiscoverRepository _discoverRepository;
    private readonly ILikeRepository _likeRepository;
    private readonly ICommentRepository _commentRepository;

    public DiscoverService(
        IDiscoverRepository discoverRepository,
        ILikeRepository likeRepository,
        ICommentRepository commentRepository)
    {
        _discoverRepository = discoverRepository;
        _likeRepository = likeRepository;
        _commentRepository = commentRepository;
    }

    public async Task<DiscoverPageDto> GetPostsAsync(Guid userId, int skip)
    {
        if (skip < 0)
            throw new ArgumentException("skip can't be negative");

        var now = DateTimeOffset.UtcNow;

        var friendIds = await _discoverRepository.GetFriendIdsAsync(userId);
        var posts = await _discoverRepository.GetCandidatePostsAsync(
            userId, friendIds, now.AddDays(-RecentDays), CandidateLimit);

        if (posts.Count == 0)
            return new DiscoverPageDto { NextSkip = skip };

        var postIds = posts.Select(p => p.Id).ToList();
        var authorIds = posts.Select(p => p.UserId).Distinct().ToList();

        // Everything the score needs, each in ONE query for the whole list (the awaits run one after another
        // on purpose: one DbContext can't run two queries at once)
        var likeCounts = await _likeRepository.CountByPostIdsAsync(postIds);
        var commentCounts = await _commentRepository.CountByPostIdsAsync(postIds);
        var mutualCounts = await _discoverRepository.GetMutualFriendCountsAsync(friendIds, authorIds);
        var tradePartners = await _discoverRepository.GetTradePartnerIdsAsync(userId, authorIds);
        var sellerOrders = await _discoverRepository.GetSellerOrderCountsAsync(authorIds, now.AddDays(-SellerDays));
        var interests = await _discoverRepository.GetInterestCategoriesAsync(userId);

        double ScoreOf(Post post, DateTimeOffset feedTime)
        {
            var mutualPoints = Math.Min(mutualCounts.GetValueOrDefault(post.UserId), MaxMutualFriends) * PointsPerMutualFriend;

            var tradePoints = tradePartners.Contains(post.UserId) ? PointsTrade : 0;

            var categoryName = post.Product?.Category?.Name?.Trim();
            var interestPoints = !string.IsNullOrEmpty(categoryName) && interests.Contains(categoryName)
                ? PointsInterest
                : 0;

            var sellerPoints = Math.Min(MaxSellerPoints, sellerOrders.GetValueOrDefault(post.UserId) * PointsPerSellerOrder);

            var engagementPoints = Math.Min(
                MaxEngagementPoints,
                likeCounts.GetValueOrDefault(post.Id) * PointsPerLike +
                commentCounts.GetValueOrDefault(post.Id) * PointsPerComment);

            var raw = mutualPoints + tradePoints + interestPoints + sellerPoints + engagementPoints;

            var ageHours = Math.Max(0.0, (now - feedTime).TotalHours);
            return raw * Math.Pow(0.5, ageHours / HalfLifeHours);
        }

        // Best score first; a tie goes to the newer post, then to the lower id so the order never jumps around
        var ranked = posts
            .Select(post =>
            {
                var feedTime = post.RevivedAt ?? post.CreatedAt;
                return new Ranked(post, feedTime, ScoreOf(post, feedTime));
            })
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.FeedTime)
            .ThenBy(r => r.Item.Id)
            .Take(PoolSize)
            .ToList();

        var page = ranked.Skip(skip).Take(PageSize).Select(r => r.Item).ToList();
        var likedByMe = await _likeRepository.GetLikedPostIdsAsync(userId, page.Select(p => p.Id).ToList());

        return new DiscoverPageDto
        {
            Items = page
                .Select(p => ToDto(
                    p,
                    likeCounts.GetValueOrDefault(p.Id),
                    commentCounts.GetValueOrDefault(p.Id),
                    likedByMe.Contains(p.Id)))
                .ToList(),
            HasMore = ranked.Count > skip + page.Count,
            NextSkip = skip + page.Count
        };
    }

    // Same shape as the posts of the news feed (this is the same mapping as PostService uses)
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

    private sealed record Ranked(Post Item, DateTimeOffset FeedTime, double Score);
}
