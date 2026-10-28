namespace TradeHub.Dtos.Ai;

// Answer = the AI's text. ToolsUsed = which data it looked at, so the user can see where the answer came from.
public record AiAskResponse(string Answer, List<string> ToolsUsed);
