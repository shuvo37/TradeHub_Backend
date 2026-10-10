namespace TradeHub.Repositories;

public interface ISearchRepository
{
    // Saves one search: a new row, or +1 on SearchCount and a new LastSearchedAt when the user searched this text before
    Task SaveAsync(Guid userId, string term, DateTimeOffset now);
    // My latest searches, newest first (shown when the search box is empty)
    Task<List<string>> GetRecentAsync(Guid userId, int take);
    // My own searches that start with this text, newest first
    Task<List<string>> GetOwnByPrefixAsync(Guid userId, string prefix, int take);
    // Searches of everybody that start with this text, most popular first.
    // A text counts only when at least 'minUsers' different people searched it, so one person's private search is never shown to others.
    Task<List<string>> GetGlobalByPrefixAsync(string prefix, int minUsers, int take);
    // Names of other people's products and categories that start with this text (lower case, no duplicates).
    // Used when there are not enough stored searches yet.
    Task<List<string>> GetCatalogNamesByPrefixAsync(Guid userId, string prefix, int take);

    // Other sellers' products where EVERY word is found in the product name, its category name or its description
    // (upper/lower case ignored). 'query' is the whole normalized text; products whose name contains it come first,
    // so the 'take' limit cuts the weakest matches. Never returns my own products.
    Task<List<ProductSearchRow>> GetProductRowsAsync(Guid userId, string query, List<string> words, int take);
    // Other sellers' categories (with at least one product) where EVERY word is found in the category name
    Task<List<CategorySearchRow>> GetCategoryRowsAsync(Guid userId, string query, List<string> words, int take);
}
