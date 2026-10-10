using TradeHub.Dtos.Posts;

namespace TradeHub.Services;

public interface IDiscoverService
{
    // Discover "For you": posts from people outside my friends list, best score first, 10 per page.
    // 'skip' is how many posts the client already has (0 for the first page).
    Task<DiscoverPageDto> GetPostsAsync(Guid userId, int skip);
}
