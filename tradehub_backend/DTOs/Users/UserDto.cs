namespace TradeHub.Dtos.Users;

// What the API returns. No Password, no tokens.
// Optional fields are never null: the frontend calls .trim() on them.
public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string PaymentNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string NecessaryInfo { get; set; } = string.Empty;
}