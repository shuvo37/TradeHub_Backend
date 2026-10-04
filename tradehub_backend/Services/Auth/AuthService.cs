using System.Security.Cryptography;
using TradeHub.Dtos.Auth;
using TradeHub.Models;
using TradeHub.Repositories;

namespace TradeHub.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private const string SuffixChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int MaxGenerationAttempts = 10;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequestDto request)
    {
        var uniqueName = await GenerateUniqueNameAsync(request.Name);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            UniqueName = uniqueName,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Avatar = request.Avatar,
            Location = request.Location,
            PaymentNumber = request.PaymentNumber,
            Email = request.Email,
            Phone = request.Phone,
            NecessaryInfo = request.NecessaryInfo,
        };

        var created = await _userRepository.AddAsync(user);
        return await IssueAsync(created);
    }

    public async Task<AuthResult?> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetByUniqueNameAsync(request.UniqueName);
        if (user is null) return null;

        var passwordMatches = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
        if (!passwordMatches) return null;

        return await IssueAsync(user);
    }

    public async Task<AuthResult?> RefreshAsync(string rawRefreshToken)
    {
        var stored = await _refreshTokenRepository.GetByHashAsync(_tokenService.Hash(rawRefreshToken));
        if (stored is null) return null;

        if (stored.RevokedAt is not null)
        {
            // An already-used token came back: possible theft, so kill all sessions of this user.
            // 10s grace so two tabs refreshing at the same moment don't log the user out.
            if (stored.RevokedAt < DateTime.UtcNow.AddSeconds(-10))
                await _refreshTokenRepository.RevokeAllForUserAsync(stored.UserId);
            return null;
        }

        if (stored.ExpiresAt < DateTime.UtcNow) return null;

        var user = await _userRepository.GetByIdAsync(stored.UserId);
        if (user is null) return null;

        // Rotation: the old token dies here, IssueAsync creates a new one
        stored.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(stored);

        return await IssueAsync(user);
    }

    public async Task LogoutAsync(string rawRefreshToken)
    {
        var stored = await _refreshTokenRepository.GetByHashAsync(_tokenService.Hash(rawRefreshToken));
        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(stored);
        }
    }

    // Creates the access token + a new refresh token (hash saved in DB, raw returned for the cookie)
    private async Task<AuthResult> IssueAsync(User user)
    {
        var (raw, hash) = _tokenService.CreateRefreshToken();
        var expiresAt = DateTime.UtcNow.Add(_tokenService.RefreshLifetime);

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
        });

        var dto = ToDto(user);
        dto.AccessToken = _tokenService.CreateAccessToken(user);
        return new AuthResult(dto, raw, expiresAt);
    }

    private async Task<string> GenerateUniqueNameAsync(string name)
    {
        var baseName = string.Concat(name.Trim().Split(' ')); // strip spaces — keeps it login-friendly

        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var candidate = $"{baseName}_{RandomSuffix()}";
            var existing = await _userRepository.GetByUniqueNameAsync(candidate);
            if (existing is null) return candidate;
        }

        throw new InvalidOperationException("Could not generate a unique name after several attempts.");
    }

    private static string RandomSuffix()
    {
        var bytes = RandomNumberGenerator.GetBytes(3);
        var chars = new char[3];
        for (var i = 0; i < 3; i++)
            chars[i] = SuffixChars[bytes[i] % SuffixChars.Length];
        return new string(chars);
    }

    private static AuthResponseDto ToDto(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        UniqueName = user.UniqueName,
        Avatar = user.Avatar,
        Location = user.Location,
        PaymentNumber = user.PaymentNumber,
        Email = user.Email,
        Phone = user.Phone,
        NecessaryInfo = user.NecessaryInfo,
    };
}