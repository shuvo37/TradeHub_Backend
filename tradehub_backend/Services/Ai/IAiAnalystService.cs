using TradeHub.Dtos.Ai;

namespace TradeHub.Services;

public interface IAiAnalystService
{
    // userId comes from the login token, never from the request body, so the AI can only ever see this user's data.
    Task<AiAskResponse> AskAsync(Guid userId, string question, List<AiChatMessage>? history);
}
