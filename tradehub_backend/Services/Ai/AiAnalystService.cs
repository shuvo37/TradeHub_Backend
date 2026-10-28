using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using TradeHub.Dtos.Ai;

namespace TradeHub.Services;

// The AI assistant for a store owner. It talks to Gemini in a loop:
//   1. send the question + the last chat messages + the list of tools
//   2. if Gemini asks for tools, run them (read-only, for THIS user) and send the results back
//   3. repeat until Gemini writes its final answer (at most MaxRounds times)
public class AiAnalystService : IAiAnalystService
{
    private const int MaxRounds = 6;
    private const int MaxHistoryQuestions = 10;   // the memory: the last 10 questions (and their answers)
    private const int MaxHistoryTextLength = 3000;

    private readonly HttpClient _http;
    private readonly AnalystTools _tools;
    private readonly GeminiSettings _settings;
    private readonly ILogger<AiAnalystService> _logger;

    public AiAnalystService(
        HttpClient http,
        AnalystTools tools,
        IOptions<GeminiSettings> options,
        ILogger<AiAnalystService> logger)
    {
        _http = http;
        _tools = tools;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<AiAskResponse> AskAsync(Guid userId, string question, List<AiChatMessage>? history)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new AiUnavailableException("The AI assistant is not set up yet");

        // The conversation so far: the remembered messages, the new question, then (tool request, tool result) pairs.
        var contents = BuildHistory(history);
        contents.Add(TextTurn("user", question.Trim()));

        var toolsUsed = new List<string>();
        var modelState = new ModelState(); // which model answered last, so one question keeps using it

        for (var round = 0; round < MaxRounds; round++)
        {
            var reply = await CallGeminiAsync(contents, modelState);

            var content = reply["candidates"]?[0]?["content"];
            var parts = content?["parts"] as JsonArray;

            if (content is null || parts is null || parts.Count == 0)
                throw new AiUnavailableException("The assistant could not answer that. Try asking in a different way.");

            var calls = parts.Where(p => p?["functionCall"] is not null).ToList();

            // No tool requested: this is the final answer.
            if (calls.Count == 0)
            {
                var answer = JoinText(parts);
                if (string.IsNullOrWhiteSpace(answer))
                    throw new AiUnavailableException("The assistant could not answer that. Try asking in a different way.");

                return new AiAskResponse(answer.Trim(), toolsUsed.Distinct().ToList());
            }

            // Keep Gemini's turn exactly as received (newer models need their hidden "thought signature" back).
            contents.Add(content.DeepClone());

            var results = new JsonArray();
            foreach (var part in calls)
            {
                var call = part!["functionCall"]!;
                var name = call["name"]?.ToString() ?? string.Empty;
                var args = call["args"] as JsonObject;

                toolsUsed.Add(name);

                JsonNode result;
                try
                {
                    // userId is the logged-in user from the token. The AI cannot choose it.
                    result = await _tools.RunAsync(userId, name, args);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AI tool {Tool} failed", name);
                    result = new JsonObject { ["error"] = "This tool failed. Continue without it and say so." };
                }

                var functionResponse = new JsonObject
                {
                    ["name"] = name,
                    ["response"] = new JsonObject { ["result"] = result }
                };

                if (call["id"] is not null)
                    functionResponse["id"] = call["id"]!.DeepClone();

                results.Add(new JsonObject { ["functionResponse"] = functionResponse });
            }

            contents.Add(new JsonObject { ["role"] = "user", ["parts"] = results });
        }

        throw new AiUnavailableException("That question needed too many steps. Try a narrower question.");
    }

    // Turns the browser's chat history into Gemini turns. The browser is not trusted: only the roles "user" and "model"
    // are accepted, texts are cut, and only the last 10 questions are kept. Gemini needs the turns to alternate
    // user / model, so anything out of order is dropped.
    private static JsonArray BuildHistory(List<AiChatMessage>? history)
    {
        var contents = new JsonArray();
        if (history is null || history.Count == 0)
            return contents;

        var clean = history
            .Where(m => !string.IsNullOrWhiteSpace(m.Text))
            .Select(m => (Role: m.Role?.Trim().ToLowerInvariant(), Text: Cut(m.Text.Trim())))
            .Where(m => m.Role == "user" || m.Role == "model")
            .ToList();

        // Keep the last 10 questions: start from the 10th "user" message counted from the end.
        var userIndexes = clean.Select((m, i) => (m, i)).Where(x => x.m.Role == "user").Select(x => x.i).ToList();
        if (userIndexes.Count > MaxHistoryQuestions)
            clean = clean.Skip(userIndexes[userIndexes.Count - MaxHistoryQuestions]).ToList();

        string? last = null;
        foreach (var (role, text) in clean)
        {
            // must start with the user and alternate
            if (last is null && role != "user") continue;
            if (role == last) continue;

            contents.Add(TextTurn(role!, text));
            last = role;
        }

        // The new question is a "user" turn, so the history must not end with one.
        if (last == "user")
            contents.RemoveAt(contents.Count - 1);

        return contents;
    }

    private static string Cut(string text) =>
        text.Length > MaxHistoryTextLength ? text[..MaxHistoryTextLength] : text;

    private static JsonObject TextTurn(string role, string text) => new()
    {
        ["role"] = role,
        ["parts"] = new JsonArray(new JsonObject { ["text"] = text })
    };

    // The model that answered the last round of THIS question. The next round tries it first: the hidden "thought
    // signatures" Gemini sends with a tool request belong to the model that wrote them.
    private sealed class ModelState
    {
        public string? Current { get; set; }
    }

    private enum Outcome
    {
        Ok,       // got an answer
        TryNext,  // busy, unreachable, or this model is not available to this key: the other model may work
        Fatal     // for example a wrong key or a bad request: another model will not help
    }

    // Asks Gemini. Tries the main model, and if it is busy (or missing) the fallback model.
    // If none of them answers, the person is told to try again in a few seconds.
    private async Task<JsonNode> CallGeminiAsync(JsonArray contents, ModelState state)
    {
        var body = new JsonObject
        {
            ["systemInstruction"] = new JsonObject
            {
                ["parts"] = new JsonArray(new JsonObject { ["text"] = BuildInstructions() })
            },
            ["contents"] = contents.DeepClone(),
            ["tools"] = JsonNode.Parse(AnalystTools.DeclarationsJson),
            // No temperature: the newer Gemini models work best with their default.
            // The limit is generous because the model's own thinking also counts toward it.
            ["generationConfig"] = new JsonObject { ["maxOutputTokens"] = 8192 }
        };
        var json = body.ToJsonString();

        var models = _settings.ModelsInOrder();
        if (models.Count == 0)
            throw new AiUnavailableException("The AI assistant is not set up yet");

        // Keep using the model that answered the previous round of this question
        if (state.Current is not null && models.Remove(state.Current))
            models.Insert(0, state.Current);

        var sawBusy = false;
        var fatal = false;

        foreach (var model in models)
        {
            var (outcome, reply, busy) = await TryModelAsync(model, json);

            if (outcome == Outcome.Ok)
            {
                state.Current = model;
                return reply!;
            }

            if (outcome == Outcome.Fatal)
            {
                fatal = true;
                break;
            }

            sawBusy |= busy;
        }

        throw new AiUnavailableException(!fatal && sawBusy
            ? "The assistant is busy right now. Please try again in a few seconds."
            : "The assistant is not available right now. Please try again later.");
    }

    // One call to one model. Never throws for a Gemini problem: it says what to do next.
    private async Task<(Outcome Outcome, JsonNode? Reply, bool Busy)> TryModelAsync(string model, string json)
    {
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/v1beta/models/{model}:generateContent";

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        // The key goes in a header, not in the URL, so it never ends up in logs.
        request.Headers.Add("x-goog-api-key", _settings.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Gemini model {Model} could not be reached", model);
            return (Outcome.TryNext, null, true);
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var reply = JsonNode.Parse(text);
                    if (reply is not null)
                        return (Outcome.Ok, reply, false);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Gemini model {Model} sent an unreadable reply", model);
                }

                return (Outcome.TryNext, null, true);
            }

            // Busy: too many requests, or a temporary Gemini problem
            var busy = response.StatusCode is HttpStatusCode.TooManyRequests
                or HttpStatusCode.InternalServerError
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.GatewayTimeout;

            // 404: this model does not exist, or is closed to this key. The other model may still work.
            var missing = response.StatusCode == HttpStatusCode.NotFound;

            if (busy || missing)
            {
                _logger.LogWarning("Gemini model {Model} returned {Status}: {Body}", model, (int)response.StatusCode, text);
                return (Outcome.TryNext, null, busy);
            }

            _logger.LogError("Gemini model {Model} returned {Status}: {Body}", model, (int)response.StatusCode, text);
            return (Outcome.Fatal, null, false);
        }
    }

    private static string JoinText(JsonArray parts)
    {
        var text = new StringBuilder();

        foreach (var part in parts)
        {
            // Skip Gemini's internal "thought" parts; keep only the real answer text.
            if (part?["thought"]?.GetValue<bool>() == true)
                continue;

            var t = part?["text"]?.ToString();
            if (!string.IsNullOrEmpty(t))
                text.Append(t);
        }

        return text.ToString();
    }

    private static string BuildInstructions()
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are the AI assistant of TradeHub, a social marketplace where every user has a personal store (categories with products) and can post, and can both sell and buy. You are talking to ONE user about THEIR OWN business.");
        sb.AppendLine($"Current date and time: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC. Money is in US dollars.");
        sb.AppendLine("- TradeHub never uses cents. Write every amount as whole dollars, like $2 or $50, never $2.40. Do not round up or add decimals yourself: use the amounts exactly as the tools give them.");
        sb.AppendLine();
        sb.AppendLine("How you work:");
        sb.AppendLine("- You can read the user's live data ONLY through the tools you are given. Call the tools you need, and several if the question needs it.");
        sb.AppendLine("- Never guess or invent products, prices or numbers. Every figure and every name in your answer must come from a tool result. If nothing matches, say so plainly. If a tool says the list was truncated, tell the user it is only part of the list and suggest a narrower question.");
        sb.AppendLine("- When the user asks for a list (for example all products of a category, or products cheaper than a price), return the rows from the tool as a simple numbered list: name, price, and stock when it is useful. Say which price you compared (list price or final price after discount) when a price filter was used.");
        sb.AppendLine("- When the user asks for analysis or advice (best sellers, slow products, low stock, what to do next), use the numbers, compare, and give a short, clear recommendation.");
        sb.AppendLine("- You cannot change anything. You only read and recommend. If the user asks you to add, edit, delete or accept something, tell them which page of TradeHub to use.");
        sb.AppendLine("- You only see the user's own data. You have no phone numbers, addresses or payment details of anyone, and you must not ask for them. Never discuss other users' stores.");
        sb.AppendLine("- Only talk about TradeHub: the user's own business data, and how to use the TradeHub app. Politely refuse anything else.");
        sb.AppendLine("- When the user asks HOW to do something in the app (order something, add a product, accept an order, ...), answer from the guide below with the exact page and button names, as short numbered steps. Never answer with vague advice like 'use the marketplace or search features'. If the guide does not cover it, say you are not sure instead of guessing.");
        sb.AppendLine("- Earlier messages of this chat are given to you, so you can follow up on them (for example 'and only the ones under $20?').");
        sb.AppendLine("- Write plain text. Short paragraphs and simple numbered lines. Do not use markdown symbols such as ** or #.");
        sb.AppendLine();
        sb.AppendLine("How TradeHub works:");
        sb.AppendLine("- A product belongs to one category. Price is the list price per item. Discount is a percent. The final price of ONE item = price x (100 - discount) / 100, rounded DOWN to whole dollars and never less than $1 (for example $3 with 20% off = $2, and $1 with 20% off is still $1). The total of an order is price x quantity x (100 - discount) / 100, rounded down once (and never less than $1), so it can be higher than quantity x the one-item final price.");
        sb.AppendLine("- Stock: a product either counts its stock (quantity; out of stock at 0) or only has a status, Available or Unavailable.");
        sb.AppendLine("- Orders: PENDING (waiting for the seller), ACCEPTED, REJECTED. Payment is either cash on delivery or paid before delivery. 'Received' orders are orders on the user's products; 'placed' orders are the user's own purchases.");
        sb.AppendLine("- Money from orders only counts when the order is ACCEPTED.");
        sb.AppendLine("- A post can promote one of the user's products and can get likes and comments.");

        sb.AppendLine();
        sb.AppendLine("How to use the TradeHub app (the guide):");
        sb.AppendLine(TradeHubGuide.Text);

        return sb.ToString();
    }
}
