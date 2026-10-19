// Dtos/ContactEmail/VerifyContactEmailDto.cs
namespace TradeHub.Dtos.ContactEmail;

// Body of POST api/contact-email/verify: the 6 digits from the mail
public class VerifyContactEmailDto
{
    // Nullable on purpose: a missing code reaches the service, which answers "Enter the 6-digit code."
    public string? Code { get; set; }
}
