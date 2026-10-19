using TradeHub.Dtos.ContactEmail;

namespace TradeHub.Services;

public interface IContactEmailService
{
    Task<ContactEmailDto> GetAsync(Guid userId);
    // Saves the email (a new address must pass the checks), and sends a code unless it is already verified
    Task<ContactEmailDto> SaveAsync(Guid userId, SaveContactEmailDto dto);
    // Sends a new code to the saved, not yet verified email
    Task<ContactEmailDto> ResendAsync(Guid userId);
    // Checks the typed code and marks the email as verified
    Task VerifyAsync(Guid userId, VerifyContactEmailDto dto);
}
