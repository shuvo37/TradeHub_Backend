using System.ComponentModel.DataAnnotations;

namespace TradeHub.Dtos.Ai;

// One earlier message of the chat. The browser keeps the last 10 questions and answers in memory and sends them
// back with every new question, so the AI can follow up ("and the ones under $20?"). Nothing is stored on the server.
public class AiChatMessage
{
    // "user" = what the person asked, "model" = what the AI answered
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class AiAskRequest
{
    [Required(ErrorMessage = "Please type a question")]
    [StringLength(500, ErrorMessage = "Question is too long (max 500 characters)")]
    public string Question { get; set; } = string.Empty;

    // Oldest first. The server keeps only the last 10 questions (with their answers).
    public List<AiChatMessage>? History { get; set; }
}
