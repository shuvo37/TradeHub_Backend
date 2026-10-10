// Dtos/Search/SaveSearchDto.cs
namespace TradeHub.Dtos.Search;

// The text the user searched for (sent when they press Enter or click a suggestion)
public class SaveSearchDto
{
    public string Term { get; set; } = string.Empty;
}
