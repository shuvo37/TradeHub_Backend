namespace TradeHub.Services;

// The AI could not answer for a reason the user can do nothing about (no key, Gemini down, empty reply).
// GlobalExceptionHandler turns it into a 503 with this message.
public class AiUnavailableException : Exception
{
    public AiUnavailableException(string message) : base(message)
    {
    }
}
