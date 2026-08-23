using System.ClientModel;
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

    // Per-model caching: each model gets its own ChatClient instance.
    private readonly Dictionary<string, ChatClient> _chatClients = new();
    private readonly object _clientLock = new();

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
        => await CompleteWithToolsAsync(messages, tools, modelOverride: null!, maxTokens: 4000, cancellationToken);

    public async Task<LlmResponse> CompleteWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        string modelOverride,
        CancellationToken cancellationToken = default)
        => await CompleteWithToolsAsync(messages, tools, modelOverride, maxTokens: 4000, cancellationToken);

    public async Task<LlmResponse> CompleteWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        string modelOverride,
        int maxTokens,
        CancellationToken cancellationToken = default)
    {
        var model = string.IsNullOrWhiteSpace(modelOverride)
            ? (string.IsNullOrWhiteSpace(_options.Model) ? OpenAiOptions.DefaultModel : _options.Model)
            : modelOverride;

        var client = GetOrCreateClientForModel(model);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var chatMessages = messages.Select(ToChatMessage).ToList();

            var chatOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = maxTokens
            };
            foreach (var tool in tools)
            {
                chatOptions.Tools.Add(ChatTool.CreateFunctionTool(
                    tool.Name,
                    tool.Description,
                    BinaryData.FromString(tool.ParametersJsonSchema)));
            }

            _logger.LogInformation(
                "🔵 Sending LLM request: Model={Model}, BaseUrl={BaseUrl}, Messages={MessageCount}, Tools={ToolCount}, MaxTokens={MaxTokens}",
                model, 
                string.IsNullOrWhiteSpace(_options.BaseUrl) ? "https://api.openai.com" : _options.BaseUrl,
                chatMessages.Count, 
                tools.Count,
                maxTokens);

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

                var usage = value.Usage;
                var totalTokens = usage?.TotalTokenCount ?? 0;

                _logger.LogInformation(
                    "🟢 LLM requested {Count} tool call(s): {Tools}. Model={Model} TotalTokens={TotalTokens} LatencyMs={LatencyMs}",
                    toolCalls.Count, string.Join(",", toolCalls.Select(t => t.Name)), model, totalTokens, stopwatch.ElapsedMilliseconds);

                return new LlmResponse { ToolCalls = toolCalls, ModelUsed = model, TotalTokens = totalTokens };
            }

            var text = value.Content.Count > 0 ? value.Content[0].Text ?? string.Empty : string.Empty;
            var finalUsage = value.Usage;
            var finalTotalTokens = finalUsage?.TotalTokenCount ?? 0;

            _logger.LogInformation(
                "🟢 LLM returned final text (length={Length}). Model={Model} TotalTokens={TotalTokens} LatencyMs={LatencyMs}",
                text.Length, model, finalTotalTokens, stopwatch.ElapsedMilliseconds);

            return new LlmResponse { FinalText = text, ModelUsed = model, TotalTokens = finalTotalTokens };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl) ? "https://api.openai.com" : _options.BaseUrl;
            
            // Extract more details from the exception
            var innerMsg = ex.InnerException?.Message ?? "No inner exception";
            var stackTrace = ex.StackTrace?.Split('\n').FirstOrDefault()?.Trim() ?? "No stack trace";
            
            _logger.LogError(ex,
                "🔴 LLM tool-calling call failed. Model={Model} LatencyMs={LatencyMs} ErrorType={ErrorType} BaseUrl={BaseUrl} " +
                "InnerException={InnerMsg} TopStackFrame={StackTrace}",
                model, stopwatch.ElapsedMilliseconds, ex.GetType().Name, baseUrl, innerMsg, stackTrace);
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
        var model = string.IsNullOrWhiteSpace(_options.Model)
            ? OpenAiOptions.DefaultModel
            : _options.Model;
        return GetOrCreateClientForModel(model);
    }

    private ChatClient GetOrCreateClientForModel(string model)
    {
        // Validate only when actually needed — not at startup
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "OpenAI:ApiKey is not configured. Add it to appsettings.json under \"OpenAI\": { \"ApiKey\": \"sk-...\" }");

        lock (_clientLock)
        {
            if (_chatClients.TryGetValue(model, out var cached))
                return cached;

            var client = BuildClient(model);
            _chatClients[model] = client;
            return client;
        }
    }

    private ChatClient BuildClient(string model)
    {
        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _logger.LogInformation(
                "🔧 Building OpenAI client with custom BaseUrl: {BaseUrl}, Model: {Model}, ApiKey: {ApiKeyPreview}...",
                _options.BaseUrl, model, 
                string.IsNullOrWhiteSpace(_options.ApiKey) ? "(empty)" : $"{_options.ApiKey[..Math.Min(10, _options.ApiKey.Length)]}...");

            var clientOptions = new OpenAIClientOptions
            {
                Endpoint = new Uri(_options.BaseUrl.TrimEnd('/') + "/")
            };
            return new OpenAIClient(new ApiKeyCredential(_options.ApiKey), clientOptions).GetChatClient(model);
        }

        _logger.LogInformation("🔧 Building OpenAI client with standard endpoint, Model: {Model}", model);
        return new OpenAIClient(new ApiKeyCredential(_options.ApiKey)).GetChatClient(model);
    }
}
