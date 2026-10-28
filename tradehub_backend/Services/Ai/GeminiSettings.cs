namespace TradeHub.Services;

// Section "Gemini" of the configuration. The real ApiKey lives in user-secrets (or an environment variable), never in git:
//   dotnet user-secrets set "Gemini:ApiKey" "<your key>"
public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;

    // The model tried first.
    public string Model { get; set; } = "gemini-3.8-flash";

    // Used when the first model is busy (or not available to this key). Leave empty to use only one model.
    public string FallbackModel { get; set; } = "gemini-3.5-flash-lite";

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

    // The models to try, in order, without empty names or duplicates.
    public List<string> ModelsInOrder() =>
        new[] { Model, FallbackModel }
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
