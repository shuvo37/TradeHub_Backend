// Dtos/ContactEmail/SaveContactEmailDto.cs
namespace TradeHub.Dtos.ContactEmail;

// Body of PUT api/contact-email (first save and later updates)
public class SaveContactEmailDto
{
    // Nullable on purpose: an empty or missing email must reach the service, which answers "Enter your email."
    public string? Email { get; set; }
}
