using TradeHub.Dtos.Friends;

namespace TradeHub.Services;

public interface ISuggestionService
{
    // "People you may know": up to 10 people, best score first. Never me, my friends or pending requests.
    Task<List<UserSuggestionDto>> GetSuggestionsAsync(Guid userId);
}
