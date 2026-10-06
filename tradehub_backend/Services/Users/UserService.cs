using System.ComponentModel.DataAnnotations;
using TradeHub.Dtos.Users;
using TradeHub.Models;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;

    public UserService(IUserRepository users)
    {
        _users = users;
    }

    public async Task<UserDto> GetMeAsync(Guid userId)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        return ToDto(user);
    }

    public async Task<UserDto> UpdateMeAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        // Validate everything first, so a bad request never half-updates the user.
        var name = Clean(dto.Name, "Name", 50);
        if (name.Length == 0) throw new ArgumentException("Name is required.");

        // UniqueName = name without spaces + "_" + the 3-char suffix generated at registration
        // (same rule as AuthService, so a name with spaces doesn't turn into a login name with spaces)
        var suffix = user.UniqueName[(user.UniqueName.LastIndexOf('_') + 1)..];
        var uniqueName = string.Concat(name.Split(' ')) + "_" + suffix;

        var avatar = Clean(dto.Avatar, "Avatar", 500);

        var email = Clean(dto.Email, "Email", 254);
        if (email.Length > 0 && !new EmailAddressAttribute().IsValid(email))
            throw new ArgumentException("Email is not valid.");

        var location = Clean(dto.Location, "Location", 100);
        var paymentNumber = Clean(dto.PaymentNumber, "Payment number", 30);
        var phone = Clean(dto.Phone, "Phone", 30);
        var necessaryInfo = Clean(dto.NecessaryInfo, "Necessary information", 1000);

        user.Name = name;
        user.UniqueName = uniqueName;
        user.Avatar = avatar;
        user.Location = location;
        user.PaymentNumber = paymentNumber;
        user.Email = email;
        user.Phone = phone;
        user.NecessaryInfo = necessaryInfo;

        if (!await _users.UpdateAsync(user))
            throw new KeyNotFoundException("User not found.");

        return ToDto(user);
    }

    // Trims; null becomes ""; rejects values over the limit.
    private static string Clean(string? value, string field, int maxLength)
    {
        var v = (value ?? string.Empty).Trim();
        if (v.Length > maxLength)
            throw new ArgumentException($"{field} is too long (max {maxLength} characters).");
        return v;
    }

    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        UniqueName = u.UniqueName,
        Avatar = u.Avatar ?? string.Empty,
        Location = u.Location ?? string.Empty,
        PaymentNumber = u.PaymentNumber ?? string.Empty,
        Email = u.Email ?? string.Empty,
        Phone = u.Phone ?? string.Empty,
        NecessaryInfo = u.NecessaryInfo ?? string.Empty,
    };
}