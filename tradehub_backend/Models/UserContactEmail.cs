namespace TradeHub.Models;

// The email we use to reach a user (confirmation codes and other messages later).
// One row per user: the key is UserId. No row means the user has not given an email yet.
// It has NO relation with User.Email (that one is just profile info and may be shared or empty).
// The same email can be saved by many users: a person with several businesses uses one email for all of them.
public class UserContactEmail
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    // Stored trimmed and in lower case
    public string Email { get; set; } = string.Empty;

    // When the user typed the confirmation code sent to this email. null = not verified.
    // Saving a different email sets it back to null.
    public DateTimeOffset? VerifiedAt { get; set; }
}
