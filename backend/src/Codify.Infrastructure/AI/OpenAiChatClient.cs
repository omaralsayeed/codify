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

    public async Task<LlmResponse> CompleteWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        var client = GetOrCreateClient();

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var chatMessages = messages.Select(ToChatMessage).ToList();

            var chatOptions = new ChatCompletionOptions();
            foreach (var tool in tools)
            {
                chatOptions.Tools.Add(ChatTool.CreateFunctionTool(
                    tool.Name,
                    tool.Description,
                    BinaryData.FromString(tool.ParametersJsonSchema)));
            }

            var completion = await client.CompleteChatAsync(chatMessages, chatOptions, cancellationToken);
            stopwatch.Stop();

            var value = completion.Value;

            // The model asked to run one or more tools — hand them back to the caller.
            if (value.FinishReason == ChatFinishReason.ToolCalls || value.ToolCalls.Count > 0)
            {
                var toolCalls = value.ToolCalls
                    .Where(tc => tc.Kind == ChatToolCallKind.Function)
                    .Select(tc => new LlmToolCall
                    {
                        Id = tc.Id,
                        Name = tc.FunctionName,
                        ArgumentsJson = tc.FunctionArguments?.ToString() ?? "{}"
                    })
                    .ToList();

                _logger.LogInformation(
                    "LLM requested {Count} tool call(s): {Tools}. LatencyMs={LatencyMs}",
                    toolCalls.Count, string.Join(",", toolCalls.Select(t => t.Name)), stopwatch.ElapsedMilliseconds);

                return new LlmResponse { ToolCalls = toolCalls };
            }

            var text = value.Content.Count > 0 ? value.Content[0].Text ?? string.Empty : string.Empty;

            _logger.LogInformation("LLM returned final text. LatencyMs={LatencyMs}", stopwatch.ElapsedMilliseconds);

            return new LlmResponse { FinalText = text };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "LLM tool-calling call failed. Model={Model} LatencyMs={LatencyMs}",
                _options.Model, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    // ── Private ───────────────────────────────────────────────────

    private static ChatMessage ToChatMessage(LlmMessage message) => message.Role switch
    {
        "system" => new SystemChatMessage(message.Content),
        "assistant" when message.ToolCalls.Count > 0 => new AssistantChatMessage(
            message.ToolCalls
                .Select(tc => ChatToolCall.CreateFunctionToolCall(tc.Id, tc.Name, BinaryData.FromString(tc.ArgumentsJson)))
                .ToArray()),
        "assistant" => new AssistantChatMessage(message.Content),
        "tool" => new ToolChatMessage(message.ToolCallId ?? string.Empty, message.Content),
        _ => new UserChatMessage(message.Content)
    };

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
