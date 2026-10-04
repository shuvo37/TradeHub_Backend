using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using TradeHub.Models;

namespace TradeHub.Services;

public interface ITokenService
{
    string CreateAccessToken(User user);
    (string Raw, string Hash) CreateRefreshToken();
    string Hash(string raw);
    TimeSpan RefreshLifetime { get; }
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public TimeSpan RefreshLifetime =>
        TimeSpan.FromDays(double.Parse(_config["Jwt:RefreshTokenDays"]!));

    public string CreateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UniqueName),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:AccessTokenMinutes"]!)),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Raw goes to the browser cookie. Hash goes to the database.
    public (string Raw, string Hash) CreateRefreshToken()
    {
        var raw = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        return (raw, Hash(raw));
    }

    public string Hash(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}