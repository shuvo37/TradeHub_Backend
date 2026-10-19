using TradeHub.Models;

namespace TradeHub.Repositories;

public interface IContactEmailRepository
{
    // The row of this user, or null when there is no row
    Task<UserContactEmail?> GetAsync(Guid userId);
    // Creates the row, or puts a different email on the existing row (and then the email is not verified any more).
    // Saving the email that is already there changes nothing.
    Task SaveAsync(Guid userId, string email);
    // Marks the email as verified, but only if the row still holds this email (it may have been changed meanwhile).
    // Returns false when it did not.
    Task<bool> MarkVerifiedAsync(Guid userId, string email, DateTimeOffset now);
}
