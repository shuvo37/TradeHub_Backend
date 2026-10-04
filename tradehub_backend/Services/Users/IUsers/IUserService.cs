
using TradeHub.Dtos.Users;

namespace TradeHub.Services;

public interface IUserService
{
    
    Task<UserDto>GetMeAsync(Guid userId);

    Task<UserDto> UpdateMeAsync(Guid userId , UpdateProfileDto dto);

    
}