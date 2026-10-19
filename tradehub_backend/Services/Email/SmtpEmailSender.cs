using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace TradeHub.Services;

// Sends mail through an SMTP server (Gmail by default). The mail shows up in the inbox with the name from
// Email:FromName and the Gmail address of the account that sends it.
public class SmtpEmailSender : IEmailSender
{
    private const int TimeoutMilliseconds = 15000;

    private readonly EmailSettings _settings;

    public SmtpEmailSender(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_settings.Username) || string.IsNullOrWhiteSpace(_settings.Password))
            throw new InvalidOperationException("Email is not set up: fill Email:Username and Email:Password.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.Username));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient { Timeout = TimeoutMilliseconds };
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
        // Google shows an app password in groups of four letters with spaces; the spaces are not part of it
        await client.AuthenticateAsync(_settings.Username, _settings.Password.Replace(" ", ""));
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
