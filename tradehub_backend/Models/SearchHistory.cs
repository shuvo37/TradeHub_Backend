namespace TradeHub.Models;

// One search text of one user (for example "iphone 13"). The key is (UserId, Term): searching the same text again
// does not add a row, it adds one to SearchCount and moves LastSearchedAt.
// Term is stored trimmed, in lower case, with single spaces, so "iPhone  13" and "iphone 13" are the same row.
// Two things read this table: the suggestions under the search box, and later the recommendations.
public class SearchHistory
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Term { get; set; } = string.Empty;

    public int SearchCount { get; set; }
    public DateTimeOffset LastSearchedAt { get; set; }
}
