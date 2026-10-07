// Services/Users/IUsers/IUserService.cs
using TradeHub.Dtos.Users;

namespace TradeHub.Services;

public interface IUserService
{
    Task<UserDto> GetMeAsync(Guid userId);

    // Any user's public profile (for visiting someone's page). Unknown id -> KeyNotFoundException.
    Task<UserDto> GetByIdAsync(Guid userId);

    Task<UserDto> UpdateMeAsync(Guid userId, UpdateProfileDto dto);

    // Find people by name or exact unique name. Never returns the searcher themself.
    Task<List<UserSummaryDto>> SearchAsync(Guid currentUserId, string? text);
}
