using System.Diagnostics;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Codify.Infrastructure.AI;

public class OpenAiChatClient(IOptions<OpenAiOptions> options, ILogger<OpenAiChatClient> logger) : ILLMClient
{
    private readonly ILogger<OpenAiChatClient> _logger = logger;
    private readonly string _model = ResolveModel(options.Value);
    private readonly ChatClient _chatClient = CreateChatClient(options.Value);

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            var response = await _chatClient.CompleteChatAsync(
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
                _model,
                inputTokens,
                outputTokens,
                stopwatch.ElapsedMilliseconds);

            return content;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "LLM call failed. Model={Model} LatencyMs={LatencyMs}", _model, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static ChatClient CreateChatClient(OpenAiOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

        var client = new OpenAIClient(options.ApiKey);
        var model = ResolveModel(options);
        return client.GetChatClient(model);
    }

    private static string ResolveModel(OpenAiOptions options) =>
        string.IsNullOrWhiteSpace(options.Model) ? OpenAiOptions.DefaultModel : options.Model;
}
