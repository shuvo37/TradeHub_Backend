using TradeHub.Enums;

namespace TradeHub.Models;

// The ACTIVE confirmation code of one user for one purpose (the key is UserId + Purpose).
// Asking for a new code replaces the row, so a new code always cancels the old one.
// Only a hash of the code is stored, never the code itself.
public class EmailCode
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public EmailCodePurpose Purpose { get; set; }

    // The address the code was sent to (lower case). A code only works for this address.
    public string Email { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    // Wrong or right, every try is counted BEFORE the code is compared, so parallel requests cannot guess for free
    public int Attempts { get; set; }
}
