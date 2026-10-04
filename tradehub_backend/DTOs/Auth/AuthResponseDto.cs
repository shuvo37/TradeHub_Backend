namespace TradeHub.Dtos.Auth;

public class AuthResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Location { get; set; }
    public string? PaymentNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? NecessaryInfo { get; set; }
     public string AccessToken { get; set; } = string.Empty;
}