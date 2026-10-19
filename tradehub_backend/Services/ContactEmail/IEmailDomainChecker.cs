namespace TradeHub.Services;

// Asks the internet whether the part after the @ is a domain that can receive mail.
public interface IEmailDomainChecker
{
    // false = the domain does not exist or has no mail server. true = it can, or we could not find out.
    Task<bool> CanReceiveMailAsync(string domain);
}
