using System.Globalization;
using DnsClient;

namespace TradeHub.Services;

// A DNS lookup: a domain can receive mail when it has an MX record (a mail server), or, if it has no MX record at all,
// an address record (A / AAAA), which mail servers fall back to. An MX record of "." (a "null MX") says the domain
// never receives mail.
// It cannot tell whether the mailbox before the @ exists: that is what the code sent to the address is for.
// If the lookup itself fails (timeout, DNS server down) the email is let through: our network problem
// must not stop a real user.
public class DnsEmailDomainChecker : IEmailDomainChecker
{
    private static readonly LookupClient Lookup = new(new LookupClientOptions
    {
        Timeout = TimeSpan.FromSeconds(3),
        Retries = 1,
        UseCache = true
    });

    private readonly ILogger<DnsEmailDomainChecker> _logger;

    public DnsEmailDomainChecker(ILogger<DnsEmailDomainChecker> logger)
    {
        _logger = logger;
    }

    public async Task<bool> CanReceiveMailAsync(string domain)
    {
        string ascii;
        try
        {
            // Domains in other scripts are looked up in their ASCII ("xn--") form
            ascii = new IdnMapping().GetAscii(domain);
        }
        catch (ArgumentException)
        {
            return false;
        }

        try
        {
            var mx = await Lookup.QueryAsync(ascii, QueryType.MX);
            if (mx.Header.ResponseCode == DnsHeaderResponseCode.NotExistentDomain) return false;

            var mxRecords = mx.Answers.MxRecords().ToList();
            if (mxRecords.Count > 0)
                return mxRecords.Any(r => r.Exchange.Value.Trim('.').Length > 0); // all "." = null MX = no mail

            // No MX record: mail servers then try the domain's own address
            var a = await Lookup.QueryAsync(ascii, QueryType.A);
            if (a.Answers.ARecords().Any()) return true;
            var aaaa = await Lookup.QueryAsync(ascii, QueryType.AAAA);
            return aaaa.Answers.AaaaRecords().Any();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DNS lookup for {Domain} failed, the email is let through", ascii);
            return true;
        }
    }
}
