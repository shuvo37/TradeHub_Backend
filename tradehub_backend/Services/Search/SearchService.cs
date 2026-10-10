using System.Text.RegularExpressions;
using TradeHub.Dtos.Products;
using TradeHub.Dtos.Search;
using TradeHub.Enums;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class SearchService : ISearchService
{
    // ---- Rules for stored searches and suggestions: change a number here and nothing else ----

    private const int MinTermLength = 2;    // a search shorter than this is not saved
    private const int MaxTermLength = 60;   // longer text is refused
    private const int SuggestionLimit = 8;  // lines under the search box
    private const int MinUsersForGlobal = 2; // a text from other people is suggested only after this many different people searched it

    // ---- The ranking policy of the search results: change a number here and nothing else ----
    //
    // Which results: products and categories of OTHER sellers (never mine) where EVERY typed word is found
    // (upper/lower case ignored): in the product name, its category name or its description for a product,
    // in the category name for a category.
    //
    // A product's points are the sum of:
    //   1) Text: the best of: name equals the whole text 40, name starts with it 30, name contains it 20,
    //      otherwise 10 (the words are spread over name, category and description)
    //   2) +15 if the category name contains the whole text, +5 if the description contains it
    //   3) Seller popularity: 1 point per order request the seller received in the last 90 days, up to 15
    //   4) Relationship with the seller: 6 per mutual friend (up to 5 friends, 30 at most),
    //      +15 if we have an order together (either direction, rejected ones ignored), +10 if the seller is my friend
    //   5) Interest: +10 if the product's category is one I ordered from or one of a product on a post I liked
    //   6) Discount: +5 if the product has a discount
    // Out of stock (a counted quantity of 0 or less, or "Unavailable" when no quantity is kept) comes AFTER all
    // in-stock products, whatever the points. Ties: product name, then id.
    //
    // A category's points are the sum of: Text (the same 40 / 30 / 20 / 10), seller popularity and relationship
    // (the same as for products), 1 point per product in the category (up to 10), +10 if I ordered from that category
    // (or liked a post with a product of it). Ties: category name, then id. The best 5 are shown, on the first page only.
    //
    // Paging: the ranked list holds at most 50 products, shown 10 per page.

    private const int MaxWords = 5;                 // only the first 5 words of the text are used
    private const int CandidateLimit = 300;         // at most this many matching products are scored
    private const int CategoryCandidateLimit = 100; // at most this many matching categories are scored
    private const int PoolSize = 50;                // the ranked product list holds at most this many
    private const int PageSize = 10;                // products per page
    private const int CategoryLimit = 5;            // categories shown

    private const int PointsNameEquals = 40;
    private const int PointsNameStarts = 30;
    private const int PointsNameContains = 20;
    private const int PointsWordsSpread = 10;
    private const int PointsCategoryContains = 15;
    private const int PointsDescriptionContains = 5;

    private const int PointsPerSellerOrder = 1;
    private const int MaxSellerPoints = 15;
    private const int SellerDays = 90;              // window for the seller's order requests

    private const int PointsPerMutualFriend = 6;
    private const int MaxMutualFriends = 5;         // 30 points at most
    private const int PointsTrade = 15;
    private const int PointsFriend = 10;

    private const int PointsInterest = 10;
    private const int PointsDiscount = 5;

    private const int PointsPerProductInCategory = 1;
    private const int MaxProductCountPoints = 10;

    private readonly ISearchRepository _repository;
    private readonly IDiscoverRepository _signals; // the same facts Discover uses: friends, mutual friends, trade, seller orders, interest

    public SearchService(ISearchRepository repository, IDiscoverRepository signals)
    {
        _repository = repository;
        _signals = signals;
    }

    public async Task SaveAsync(Guid userId, SaveSearchDto dto)
    {
        var term = Normalize(dto.Term);

        if (term.Length < MinTermLength)
            throw new ArgumentException($"Search text must be at least {MinTermLength} letters.");
        if (term.Length > MaxTermLength)
            throw new ArgumentException($"Search text is too long (max {MaxTermLength} characters).");

        await _repository.SaveAsync(userId, term, DateTimeOffset.UtcNow);
    }

    // Suggestions come from three places, in this order, until there are 8 lines:
    //   1) my own searches that start with the typed text (newest first)
    //   2) searches of other people that start with it (most searched first; only texts searched by at least 2 people)
    //   3) names of other sellers' products and categories that start with it (so the box also works on day one)
    // The same text is never listed twice.
    public async Task<List<string>> GetSuggestionsAsync(Guid userId, string? text)
    {
        var prefix = Normalize(text);

        // Nothing typed: show what I searched most recently
        if (prefix.Length == 0)
            return await _repository.GetRecentAsync(userId, SuggestionLimit);

        // Nothing is stored that long, so there is nothing to suggest
        if (prefix.Length > MaxTermLength)
            return new List<string>();

        var result = new List<string>();

        AddNew(result, await _repository.GetOwnByPrefixAsync(userId, prefix, SuggestionLimit));

        if (result.Count < SuggestionLimit)
            AddNew(result, await _repository.GetGlobalByPrefixAsync(prefix, MinUsersForGlobal, SuggestionLimit));

        if (result.Count < SuggestionLimit)
            AddNew(result, await _repository.GetCatalogNamesByPrefixAsync(userId, prefix, SuggestionLimit));

        return result;
    }

    public async Task<SearchResultDto> SearchAsync(Guid userId, string? text, int skip)
    {
        if (skip < 0)
            throw new ArgumentException("skip can't be negative");

        var query = Normalize(text);
        if (query.Length < MinTermLength)
            throw new ArgumentException($"Search text must be at least {MinTermLength} letters.");
        if (query.Length > MaxTermLength)
            throw new ArgumentException($"Search text is too long (max {MaxTermLength} characters).");

        var words = query.Split(' ').Take(MaxWords).ToList();
        var now = DateTimeOffset.UtcNow;

        var productRows = await _repository.GetProductRowsAsync(userId, query, words, CandidateLimit);
        // The categories are only on the first page
        var categoryRows = skip == 0
            ? await _repository.GetCategoryRowsAsync(userId, query, words, CategoryCandidateLimit)
            : new List<CategorySearchRow>();

        if (productRows.Count == 0 && categoryRows.Count == 0)
            return new SearchResultDto { NextSkip = skip };

        // Facts about the sellers, each in ONE query for all of them (the awaits run one after another on purpose:
        // one DbContext can't run two queries at once)
        var sellerIds = productRows.Select(r => r.SellerId)
            .Concat(categoryRows.Select(r => r.SellerId))
            .Distinct()
            .ToList();

        var friendIds = await _signals.GetFriendIdsAsync(userId);
        var mutualCounts = await _signals.GetMutualFriendCountsAsync(friendIds, sellerIds);
        var tradePartners = await _signals.GetTradePartnerIdsAsync(userId, sellerIds);
        var sellerOrders = await _signals.GetSellerOrderCountsAsync(sellerIds, now.AddDays(-SellerDays));
        var interests = await _signals.GetInterestCategoriesAsync(userId);
        var friendSet = friendIds.ToHashSet();

        // Seller popularity + relationship: the same for products and categories of one seller
        int SellerPoints(Guid sellerId) =>
            Math.Min(MaxSellerPoints, sellerOrders.GetValueOrDefault(sellerId) * PointsPerSellerOrder) +
            Math.Min(mutualCounts.GetValueOrDefault(sellerId), MaxMutualFriends) * PointsPerMutualFriend +
            (tradePartners.Contains(sellerId) ? PointsTrade : 0) +
            (friendSet.Contains(sellerId) ? PointsFriend : 0);

        bool IsInterest(string categoryName) => interests.Contains(categoryName.Trim());

        var rankedProducts = productRows
            .Select(row =>
            {
                var points = TextPoints(row.Name, query);
                if (row.CategoryName.ToLowerInvariant().Contains(query)) points += PointsCategoryContains;
                if (row.Description.ToLowerInvariant().Contains(query)) points += PointsDescriptionContains;
                points += SellerPoints(row.SellerId);
                if (IsInterest(row.CategoryName)) points += PointsInterest;
                if (row.Discount > 0) points += PointsDiscount;
                return new RankedProduct(row, points, IsOutOfStock(row));
            })
            .OrderBy(x => x.OutOfStock)                                      // in stock first
            .ThenByDescending(x => x.Points)
            .ThenBy(x => x.Row.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Row.ProductId)
            .Take(PoolSize)
            .ToList();

        var page = rankedProducts.Skip(skip).Take(PageSize).ToList();

        var topCategories = categoryRows
            .Select(row =>
            {
                var points = TextPoints(row.Name, query);
                points += SellerPoints(row.SellerId);
                points += Math.Min(MaxProductCountPoints, row.ProductCount * PointsPerProductInCategory);
                if (IsInterest(row.Name)) points += PointsInterest;
                return new RankedCategory(row, points);
            })
            .OrderByDescending(x => x.Points)
            .ThenBy(x => x.Row.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Row.CategoryId)
            .Take(CategoryLimit)
            .ToList();

        return new SearchResultDto
        {
            Products = page.Select(x => ToProductDto(x.Row)).ToList(),
            Categories = topCategories.Select(x => ToCategoryDto(x.Row)).ToList(),
            HasMore = rankedProducts.Count > skip + page.Count,
            NextSkip = skip + page.Count
        };
    }

    // How well a name matches the whole typed text: equals 40, starts with 30, contains 20,
    // otherwise 10 (every word is there, but spread over several fields or in another order)
    private static int TextPoints(string name, string query)
    {
        var lower = name.Trim().ToLowerInvariant();
        if (lower == query) return PointsNameEquals;
        if (lower.StartsWith(query, StringComparison.Ordinal)) return PointsNameStarts;
        if (lower.Contains(query, StringComparison.Ordinal)) return PointsNameContains;
        return PointsWordsSpread;
    }

    // Same stock rule as ordering: a counted quantity decides; without one, only "Unavailable" means out of stock
    private static bool IsOutOfStock(ProductSearchRow row)
    {
        if (row.Quantity != null) return row.Quantity <= 0;
        return row.Status == StockStatus.Unavailable;
    }

    private static ProductSearchItemDto ToProductDto(ProductSearchRow row) => new()
    {
        Product = new ProductDto
        {
            Id = row.ProductId,
            Name = row.Name,
            Price = row.Price,
            Description = row.Description,
            Image = row.Image,
            Quantity = row.Quantity,
            Status = row.Status,
            Discount = row.Discount
        },
        CategoryId = row.CategoryId,
        CategoryName = row.CategoryName,
        SellerId = row.SellerId,
        SellerName = row.SellerName,
        SellerAvatar = row.SellerAvatar
    };

    private static CategorySearchItemDto ToCategoryDto(CategorySearchRow row) => new()
    {
        Id = row.CategoryId,
        Name = row.Name,
        SellerId = row.SellerId,
        SellerName = row.SellerName,
        SellerAvatar = row.SellerAvatar,
        ProductCount = row.ProductCount
    };

    private sealed record RankedProduct(ProductSearchRow Row, int Points, bool OutOfStock);
    private sealed record RankedCategory(CategorySearchRow Row, int Points);

    // Adds the texts that are not in the list yet, until the list has 8 lines
    private static void AddNew(List<string> result, List<string> found)
    {
        foreach (var item in found)
        {
            if (result.Count >= SuggestionLimit) return;
            if (!result.Contains(item)) result.Add(item);
        }
    }

    // trimmed, lower case, every run of spaces becomes one space: "  iPhone   13 " -> "iphone 13"
    private static string Normalize(string? text)
    {
        var lower = (text ?? string.Empty).Trim().ToLowerInvariant();
        return Regex.Replace(lower, @"\s+", " ");
    }
}
