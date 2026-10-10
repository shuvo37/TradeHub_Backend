using TradeHub.Dtos.Friends;
using TradeHub.Enums;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class SuggestionService : ISuggestionService
{
    // ---- The ranking formula: change a number here and nothing else ----

    // 1) Mutual friends: points per friend, and how many friends count at most
    private const int PointsPerMutualFriend = 10;
    private const int MaxMutualFriends = 5;            // 50 points at most

    // 2) Orders between us (either direction, not rejected)
    private const int PointsFirstOrder = 25;
    private const int PointsExtraOrder = 5;
    private const int MaxOrderPoints = 35;

    // 3) Likes and comments between us (either direction)
    private const int PointsPerLike = 1;
    private const int PointsPerComment = 3;
    private const int MaxInteractionPoints = 20;

    // 4) Seller popularity: order requests received in the recent period
    private const int PointsPerSellerOrder = 1;
    private const int MaxSellerPoints = 20;

    // The window for comments and order requests (likes have no date)
    private const int RecentDays = 90;

    // How many people the list holds, and how many top sellers join the pool
    private const int ListSize = 10;
    private const int TopSellerPool = 20;

    private readonly ISuggestionRepository _repository;

    public SuggestionService(ISuggestionRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<UserSuggestionDto>> GetSuggestionsAsync(Guid userId)
    {
        var excluded = await _repository.GetExcludedIdsAsync(userId);
        var since = DateTimeOffset.UtcNow.AddDays(-RecentDays);

        var signals = await _repository.GetSignalsAsync(userId, excluded, since, TopSellerPool);

        // Best score first; a tie goes to more mutual friends, then to the lower id so the order never jumps around
        var top = signals
            .Select(Score)
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Signals.MutualFriends)
            .ThenBy(x => x.Signals.UserId)
            .Take(ListSize)
            .ToList();

        // Too few people with a score (a brand new app): fill with sellers who have the most products
        var fillIds = new List<Guid>();
        if (top.Count < ListSize)
        {
            var alreadyUsed = excluded.Concat(top.Select(x => x.Signals.UserId)).ToList();
            fillIds = await _repository.GetActiveSellerIdsAsync(alreadyUsed, ListSize - top.Count);
        }

        var ids = top.Select(x => x.Signals.UserId).Concat(fillIds).ToList();
        if (ids.Count == 0) return new List<UserSuggestionDto>();

        var users = (await _repository.GetUsersAsync(ids)).ToDictionary(u => u.Id);
        var result = new List<UserSuggestionDto>();

        foreach (var item in top)
        {
            if (!users.TryGetValue(item.Signals.UserId, out var user)) continue;
            result.Add(ToDto(user, item.Signals.MutualFriends, item.Reason));
        }

        foreach (var id in fillIds)
        {
            if (!users.TryGetValue(id, out var user)) continue;
            result.Add(ToDto(user, 0, "Active seller"));
        }

        return result;
    }

    // Turns the counts of one person into points, and picks the biggest part as the reason
    private static Scored Score(SuggestionSignals s)
    {
        var mutualPoints = Math.Min(s.MutualFriends, MaxMutualFriends) * PointsPerMutualFriend;

        var orders = s.OrdersIBought + s.OrdersTheyBought;
        var orderPoints = orders == 0
            ? 0
            : Math.Min(MaxOrderPoints, PointsFirstOrder + (orders - 1) * PointsExtraOrder);

        var likes = s.LikesToMe + s.LikesFromMe;
        var comments = s.CommentsToMe + s.CommentsFromMe;
        var interactionPoints = Math.Min(MaxInteractionPoints, likes * PointsPerLike + comments * PointsPerComment);

        var sellerPoints = Math.Min(MaxSellerPoints, s.SellerOrders * PointsPerSellerOrder);

        var total = mutualPoints + orderPoints + interactionPoints + sellerPoints;
        return new Scored(s, total, BuildReason(s, mutualPoints, orderPoints, interactionPoints, sellerPoints));
    }

    // The biggest part of the score wins; on a tie the order is mutual friends, orders, interaction, popularity
    private static string BuildReason(SuggestionSignals s, int mutualPoints, int orderPoints, int interactionPoints, int sellerPoints)
    {
        var best = Math.Max(Math.Max(mutualPoints, orderPoints), Math.Max(interactionPoints, sellerPoints));

        if (mutualPoints == best)
            return s.MutualFriends == 1 ? "1 mutual friend" : $"{s.MutualFriends} mutual friends";

        if (orderPoints == best)
        {
            if (s.OrdersIBought > 0 && s.OrdersTheyBought > 0) return "You have traded together";
            return s.OrdersIBought > 0 ? "You ordered from them" : "They ordered from you";
        }

        if (interactionPoints == best)
        {
            var toMe = s.LikesToMe + s.CommentsToMe;
            return toMe > 0 ? "Interacted with your posts" : "You interacted with their posts";
        }

        return "Popular seller";
    }

    private static UserSuggestionDto ToDto(Models.User user, int mutualCount, string reason)
    {
        return new UserSuggestionDto
        {
            Id = user.Id,
            Name = user.Name,
            UniqueName = user.UniqueName,
            Avatar = user.Avatar ?? string.Empty,
            Location = user.Location ?? string.Empty,
            FriendStatus = FriendStatus.None,
            RequestId = null,
            MutualCount = mutualCount,
            Reason = reason
        };
    }

    private sealed record Scored(SuggestionSignals Signals, int Score, string Reason);
}
