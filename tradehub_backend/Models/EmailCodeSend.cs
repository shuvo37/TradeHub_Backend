using TradeHub.Enums;

namespace TradeHub.Models;

// One line per code mail sent (or tried). It only exists for the limits: the wait between two codes,
// and how many codes one account / one address may ask for per hour.
public class EmailCodeSend
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public EmailCodePurpose Purpose { get; set; }

    // The address the code was sent to (lower case)
    public string Email { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; }
}
