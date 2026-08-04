using System.Diagnostics;
using System.Text.Json;
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

    public async Task<LlmResponse> CompleteWithToolsAsync(
        List<LlmMessage> messages,
        List<LlmToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Map our message types to OpenAI SDK types.
            var chatMessages = messages.Select(MapToChatMessage).ToList();

            // Build tool definitions for the OpenAI SDK.
            var chatTools = tools.Select(t => ChatTool.CreateFunctionTool(
                t.Name,
                t.Description,
                BinaryData.FromString(t.ParametersJsonSchema))).ToList();

            var options = new ChatCompletionOptions();
            foreach (var tool in chatTools)
                options.Tools.Add(tool);

            // Force tool choice to "auto" so the model decides.
            options.ToolChoice = ChatToolChoice.CreateAutoChoice();

            var response = await _chatClient.CompleteChatAsync(
                chatMessages, options, cancellationToken);

            stopwatch.Stop();

            var usage = response.Value.Usage;
            _logger.LogInformation(
                "LLM tool-call success. Model={Model} InputTokens={InputTokens} OutputTokens={OutputTokens} LatencyMs={LatencyMs} HasToolCalls={HasToolCalls}",
                _model,
                usage?.InputTokenCount ?? 0,
                usage?.OutputTokenCount ?? 0,
                stopwatch.ElapsedMilliseconds,
                response.Value.ToolCalls.Count > 0);

            // Check if the model wants to call tools.
            if (response.Value.ToolCalls.Count > 0)
            {
                var toolCalls = response.Value.ToolCalls.Select(tc => new LlmToolCall
                {
                    Id = tc.Id,
                    Name = tc.FunctionName,
                    ArgumentsJson = tc.FunctionArguments.ToString()
                }).ToList();

                return new LlmResponse { HasToolCalls = true, ToolCalls = toolCalls };
            }

            // Model returned final text.
            var finalText = response.Value.Content.Count > 0
                ? response.Value.Content[0].Text ?? string.Empty
                : string.Empty;

            return new LlmResponse { HasToolCalls = false, FinalText = finalText };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "LLM tool-call failed. Model={Model} LatencyMs={LatencyMs}",
                _model, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static ChatMessage MapToChatMessage(LlmMessage message)
    {
        // Tool result messages.
        if (message.Role == "tool" && message.ToolCallId is not null)
        {
            return new ToolChatMessage(message.ToolCallId, message.Content ?? string.Empty);
        }

        if (message.Role == "assistant" && message.ToolCalls is not null && message.ToolCalls.Count > 0)
        {
            var sdkToolCalls = message.ToolCalls.Select(tc => ChatToolCall.CreateFunctionToolCall(
                tc.Id,
                tc.Name,
                BinaryData.FromString(tc.ArgumentsJson))).ToList();
            return new AssistantChatMessage(sdkToolCalls);
        }

        if (message.Role == "system")
            return new SystemChatMessage(message.Content ?? string.Empty);

        if (message.Role == "user")
            return new UserChatMessage(message.Content ?? string.Empty);

        // Default: treat as user message.
        return new UserChatMessage(message.Content ?? string.Empty);
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
