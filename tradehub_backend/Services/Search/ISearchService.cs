using TradeHub.Dtos.Search;

namespace TradeHub.Services;

public interface ISearchService
{
    // The user searched this text (Enter, or a click on a suggestion): remember it
    Task SaveAsync(Guid userId, SaveSearchDto dto);
    // The lines under the search box. Empty text: my latest searches. Otherwise: texts that start with what was typed.
    Task<List<string>> GetSuggestionsAsync(Guid userId, string? text);
    // Products and categories of other sellers that match the text, best first. Products come 10 per page
    // ('skip' = how many the client already has, 0 for the first page); categories (up to 5) only on the first page.
    Task<SearchResultDto> SearchAsync(Guid userId, string? text, int skip);
}
