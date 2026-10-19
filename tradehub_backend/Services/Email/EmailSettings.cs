namespace TradeHub.Services;

// Section "Email" of the settings. Keep the real values in user-secrets (or environment variables), never in git:
//   dotnet user-secrets set "Email:Username" "yourname@gmail.com"
//   dotnet user-secrets set "Email:Password" "<16-letter Google app password>"
public class EmailSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    // The Gmail address that sends the mail. People see it as the sender in their inbox.
    public string Username { get; set; } = string.Empty;
    // A Google "app password" (not the normal Gmail password). Spaces in it are ignored.
    public string Password { get; set; } = string.Empty;
    // The name shown next to the address in the inbox
    public string FromName { get; set; } = "TradeHub";
}
