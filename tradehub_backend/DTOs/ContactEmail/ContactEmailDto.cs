// Dtos/ContactEmail/ContactEmailDto.cs
namespace TradeHub.Dtos.ContactEmail;

// Where I stand with my contact email. Answer of GET api/contact-email, and of PUT and POST resend.
//   Email null                   no email saved yet: the app shows the email form
//   Email set, Verified false    the email is saved but not confirmed: the app shows the code step
//   Verified true                confirmed: the app opens
// ExpiresInSeconds = how long the current code still works (0 = no working code, a new one must be sent).
// ResendInSeconds = how long until a new code can be asked (0 = now). Both are counted by the server.
public class ContactEmailDto
{
    public string? Email { get; set; }
    public bool Verified { get; set; }
    public int ExpiresInSeconds { get; set; }
    public int ResendInSeconds { get; set; }
}
