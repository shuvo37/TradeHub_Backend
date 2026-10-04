namespace TradeHub.Dtos.Users;

// What the client may change. No Id, UniqueName or Password on purpose.
public class UpdateProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Location { get; set; }
    public string? PaymentNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? NecessaryInfo { get; set; }
}