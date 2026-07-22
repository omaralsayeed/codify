using System.Diagnostics;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Codify.Infrastructure.AI;

public class OpenAiChatClient(
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiChatClient> logger) : ILLMClient
{
    private readonly ILogger<OpenAiChatClient> _logger = logger;
    private readonly OpenAiOptions _options = options.Value;

    // Lazily created — we don't touch the API key until the first actual call.
    // This means the app starts successfully even when the key is not configured,
    // and only fails at call-time (which gets caught by each agent's try/catch).
    private ChatClient? _chatClient;

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var client = GetOrCreateClient();

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            var response = await client.CompleteChatAsync(
                messages,
                null,
                cancellationToken);

            stopwatch.Stop();

            var content = response.Value.Content.Count > 0
                ? response.Value.Content[0].Text ?? string.Empty
                : string.Empty;

            var usage = response.Value.Usage;
            var inputTokens = usage?.InputTokenCount ?? 0;
            var outputTokens = usage?.OutputTokenCount ?? 0;

            _logger.LogInformation(
                "LLM call success. Model={Model} InputTokens={InputTokens} OutputTokens={OutputTokens} LatencyMs={LatencyMs}",
                _options.Model, inputTokens, outputTokens, stopwatch.ElapsedMilliseconds);

            return content;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "LLM call failed. Model={Model} LatencyMs={LatencyMs}",
                _options.Model, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    // ── Private ───────────────────────────────────────────────────

    private ChatClient GetOrCreateClient()
    {
        // Validate only when actually needed — not at startup
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "OpenAI:ApiKey is not configured. Add it to appsettings.json under \"OpenAI\": { \"ApiKey\": \"sk-...\" }");

        if (_chatClient is not null)
            return _chatClient;

        var model = string.IsNullOrWhiteSpace(_options.Model)
            ? OpenAiOptions.DefaultModel
            : _options.Model;

        _chatClient = new OpenAIClient(_options.ApiKey).GetChatClient(model);
        return _chatClient;
    }
}
