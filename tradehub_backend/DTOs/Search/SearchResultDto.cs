// Dtos/Search/SearchResultDto.cs
namespace TradeHub.Dtos.Search;

// One page of search results. Products are ranked best first, 10 per page, from a ranked list of at most 50,
// so the page is found by counting (skip), not by a timestamp.
public class SearchResultDto
{
    public List<ProductSearchItemDto> Products { get; set; } = new();

    // Up to 5 matching categories. Only the first page (skip = 0) has them; the next pages send an empty list.
    public List<CategorySearchItemDto> Categories { get; set; } = new();

    public bool HasMore { get; set; }

    // Where the next page starts: the client sends it back unchanged as ?skip=... when "More" is pressed
    public int NextSkip { get; set; }
}
