namespace TradeHub.Services;

// Sends one plain-text email. The rest of the app only knows this interface, so changing the provider
// (Gmail today, something else later) means writing one new class and changing one line in Program.cs.
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body);
}
