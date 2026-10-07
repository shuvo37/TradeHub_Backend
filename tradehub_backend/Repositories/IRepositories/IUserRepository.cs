using TradeHub.Models;

namespace TradeHub.Repositories;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUniqueNameAsync(string uniqueName);
    // People matching the text: the exact unique name first, then names containing it. Never includes excludeUserId.
    Task<List<User>> SearchAsync(string text, Guid excludeUserId, int take);
    Task<User> AddAsync(User user);
    Task<bool> UpdateAsync(User user);
    Task<bool> DeleteAsync(Guid id);
}
