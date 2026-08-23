using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Codify.Infrastructure.AI;

/// <summary>
/// Custom LLM client for ITI's custom API that expects requests in a specific format.
/// Unlike OpenAI SDK, this makes direct HTTP calls to /student/chat endpoint.
/// </summary>
public class CustomApiChatClient(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<CustomApiChatClient> logger) : ILLMClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<CustomApiChatClient> _logger = logger;
    private readonly OpenAiOptions _options = options.Value;

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var payload = new
            {
                model_id = string.IsNullOrWhiteSpace(_options.Model) 
                    ? OpenAiOptions.DefaultModel 
                    : _options.Model,
                messages = new[]
                {
                    new { role = "user", content = userMessage }
                },
                system_prompt = systemPrompt,
                max_tokens = 2000
            };

            _logger.LogInformation(
                "🔵 Sending LLM request to /student/chat: Model={Model}",
                payload.model_id);
            _logger.LogInformation("📤 [DEBUG] Request Payload (Simple):\nModel: {Model}, Messages Count: {MsgCount}, SystemPrompt Length: {SysLen}",
                payload.model_id, payload.messages.Length, payload.system_prompt?.Length ?? 0);

            var response = await _httpClient.PostAsJsonAsync("chat", payload, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "🔴 LLM API returned {StatusCode}: {Error}",
                    response.StatusCode,
                    errorBody);
                throw new InvalidOperationException(
                    $"LLM API failed with status {response.StatusCode}: {errorBody}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("📦 [DEBUG] Raw API Response (Simple Completion):\n{RawResponse}", jsonResponse);
            
            var result = JsonSerializer.Deserialize<CustomApiResponse>(jsonResponse);
            _logger.LogInformation("✅ [DEBUG] Parsed - OutputText Length: {Length}", result?.OutputText?.Length ?? 0);
            
            if (result?.Usage != null)
            {
                _logger.LogInformation("✅ [DEBUG] Usage - Total: {Total}, Input: {Input}, Output: {Output}",
                    result.Usage.TotalTokens, result.Usage.InputTokens, result.Usage.OutputTokens);
            }

            stopwatch.Stop();

            var content = result?.OutputText ?? string.Empty;

            _logger.LogInformation(
                "🟢 LLM call success. Model={Model} LatencyMs={LatencyMs}",
                payload.model_id, stopwatch.ElapsedMilliseconds);

            return content;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "🔴 LLM call failed. LatencyMs={LatencyMs}",
                stopwatch.ElapsedMilliseconds);
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

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Extract system prompt from messages
            var systemMessage = messages.FirstOrDefault(m => m.Role == "system");
            var systemPrompt = systemMessage?.Content ?? "You are a helpful assistant.";

            // Convert messages to custom API format (exclude system message as it goes in system_prompt)
            var apiMessages = messages
                .Where(m => m.Role != "system")
                .Select(m => ConvertToApiMessage(m))
                .ToList();

            // Convert tools to custom API format
            var apiTools = tools.Select(t => new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = JsonSerializer.Deserialize<JsonObject>(t.ParametersJsonSchema)
                }
            }).ToList();

            var payload = new
            {
                model_id = model,
                messages = apiMessages,
                system_prompt = systemPrompt,
                tools = apiTools,
                tool_choice = apiTools.Count > 0 ? "auto" : (object?)null,
                max_tokens = maxTokens
            };

            _logger.LogInformation(
                "🔵 Sending LLM request to /student/chat: Model={Model}, Messages={MessageCount}, Tools={ToolCount}, MaxTokens={MaxTokens}",
                model, apiMessages.Count, tools.Count, maxTokens);
            _logger.LogInformation("📤 [DEBUG] Request Payload (Tool-Calling):\nModel: {Model}, ApiMessages Count: {MsgCount}, Tools Count: {ToolCount}, MaxTokens: {MaxTokens}, SystemPrompt Length: {SysLen}",
                model, apiMessages.Count, apiTools.Count, maxTokens, systemPrompt?.Length ?? 0);

            var response = await _httpClient.PostAsJsonAsync("chat", payload, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "🔴 LLM API returned {StatusCode}: {Error}",
                    response.StatusCode,
                    errorBody);
                throw new InvalidOperationException(
                    $"LLM API failed with status {response.StatusCode}: {errorBody}");
            }

            stopwatch.Stop();

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("📦 [DEBUG] Raw API Response (Tool-Calling):\n{RawResponse}", jsonResponse);
            
            var result = JsonSerializer.Deserialize<CustomApiResponse>(jsonResponse);
            _logger.LogInformation("✅ [DEBUG] Parsed Response - OutputText Length: {Length}", result?.OutputText?.Length ?? 0);
            
            if (result?.Usage != null)
            {
                _logger.LogInformation("✅ [DEBUG] Usage - Total: {Total}, Input: {Input}, Output: {Output}",
                    result.Usage.TotalTokens, result.Usage.InputTokens, result.Usage.OutputTokens);
            }

            if (string.IsNullOrEmpty(result?.OutputText))
            {
                _logger.LogWarning("🟡 [DEBUG] API returned empty output_text");
                return new LlmResponse { FinalText = string.Empty, ModelUsed = model, TotalTokens = 0 };
            }

            var outputText = result.OutputText;
            _logger.LogInformation("✅ [DEBUG] Output Text:\n{OutputText}", outputText);

            // Check if output contains tool calls in XML format: <tool_calls>...</tool_calls>
            if (outputText.Contains("<tool_calls>") && outputText.Contains("</tool_calls>"))
            {
                _logger.LogInformation("🔧 [DEBUG] Detected tool calls in XML format");
                var toolCalls = ParseToolCallsFromXml(outputText);
                
                if (toolCalls.Count > 0)
                {
                    var totalTokens = result.Usage?.TotalTokens ?? 0;

                    _logger.LogInformation(
                        "🟢 LLM requested {Count} tool call(s): {Tools}. Model={Model} TotalTokens={TotalTokens} LatencyMs={LatencyMs}",
                        toolCalls.Count, string.Join(",", toolCalls.Select(t => t.Name)), model, totalTokens, stopwatch.ElapsedMilliseconds);

                    return new LlmResponse { ToolCalls = toolCalls, ModelUsed = model, TotalTokens = totalTokens };
                }
            }

            // No tool calls - return final text
            var totalTokensFinal = result.Usage?.TotalTokens ?? 0;

            _logger.LogInformation(
                "🟢 LLM returned final text (length={Length}). Model={Model} TotalTokens={TotalTokens} LatencyMs={LatencyMs}",
                outputText.Length, model, totalTokensFinal, stopwatch.ElapsedMilliseconds);

            return new LlmResponse { FinalText = outputText, ModelUsed = model, TotalTokens = totalTokensFinal };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var innerMsg = ex.InnerException?.Message ?? "No inner exception";
            var stackTrace = ex.StackTrace?.Split('\n').FirstOrDefault()?.Trim() ?? "No stack trace";
            
            _logger.LogError(ex,
                "🔴 LLM tool-calling call failed. Model={Model} LatencyMs={LatencyMs} ErrorType={ErrorType} " +
                "InnerException={InnerMsg} TopStackFrame={StackTrace}",
                model, stopwatch.ElapsedMilliseconds, ex.GetType().Name, innerMsg, stackTrace);
            throw;
        }
    }

    // ── Private ───────────────────────────────────────────────────

    private static object ConvertToApiMessage(LlmMessage message)
    {
        // Handle tool call messages (assistant calling tools)
        if (message.Role == "assistant" && message.ToolCalls.Count > 0)
        {
            return new
            {
                role = "assistant",
                content = message.Content,
                tool_calls = message.ToolCalls.Select(tc => new
                {
                    id = tc.Id,
                    type = "function",
                    function = new
                    {
                        name = tc.Name,
                        arguments = tc.ArgumentsJson
                    }
                }).ToArray()
            };
        }

        // Handle tool result messages
        // AWS Bedrock Converse API doesn't support role="tool"
        // Tool results must be sent as user messages with the tool result content
        if (message.Role == "tool")
        {
            return new
            {
                role = "user",
                content = new[]
                {
                    new
                    {
                        toolResult = new
                        {
                            toolUseId = message.ToolCallId,
                            content = new[]
                            {
                                new { text = message.Content }
                            }
                        }
                    }
                }
            };
        }

        // Standard message
        return new
        {
            role = message.Role,
            content = message.Content
        };
    }

    /// <summary>
    /// Parses tool calls from Anthropic XML format: 
    /// &lt;tool_calls&gt;&lt;invoke name="tool_name"&gt;&lt;parameter name="arg"&gt;value&lt;/parameter&gt;&lt;/invoke&gt;&lt;/tool_calls&gt;
    /// </summary>
    private List<LlmToolCall> ParseToolCallsFromXml(string outputText)
    {
        var toolCalls = new List<LlmToolCall>();

        try
        {
            // Extract content between <tool_calls> and </tool_calls>
            var startTag = "<tool_calls>";
            var endTag = "</tool_calls>";
            var startIndex = outputText.IndexOf(startTag);
            var endIndex = outputText.IndexOf(endTag);

            if (startIndex < 0 || endIndex < 0)
            {
                _logger.LogWarning("🟡 [DEBUG] Could not find tool_calls tags in output");
                return toolCalls;
            }

            var toolCallsSection = outputText.Substring(
                startIndex + startTag.Length,
                endIndex - startIndex - startTag.Length);

            _logger.LogInformation("✅ [DEBUG] Extracted tool_calls section:\n{Section}", toolCallsSection);

            // Parse <invoke name="..."> tags (Anthropic format)
            var invokeTag = "<invoke name=\"";
            var invokeEndTag = "</invoke>";
            var currentIndex = 0;

            while (true)
            {
                var invokeStartIndex = toolCallsSection.IndexOf(invokeTag, currentIndex);
                if (invokeStartIndex < 0) break;

                // Extract tool name from <invoke name="tool_name">
                var nameStart = invokeStartIndex + invokeTag.Length;
                var nameEnd = toolCallsSection.IndexOf("\"", nameStart);
                if (nameEnd < 0) break;

                var toolName = toolCallsSection.Substring(nameStart, nameEnd - nameStart);

                var invokeCloseTag = toolCallsSection.IndexOf(">", nameEnd);
                if (invokeCloseTag < 0) break;

                var invokeEnd = toolCallsSection.IndexOf(invokeEndTag, invokeCloseTag);
                if (invokeEnd < 0) break;

                // Extract parameters section
                var parametersSection = toolCallsSection.Substring(
                    invokeCloseTag + 1,
                    invokeEnd - invokeCloseTag - 1);

                _logger.LogInformation("✅ [DEBUG] Parsing invoke: Name={Name}, Parameters section:\n{Params}", 
                    toolName, parametersSection);

                // Parse <parameter name="...">value</parameter> tags
                var arguments = new Dictionary<string, object>();
                var paramTag = "<parameter name=\"";
                var paramIndex = 0;

                while (true)
                {
                    var paramStart = parametersSection.IndexOf(paramTag, paramIndex);
                    if (paramStart < 0) break;

                    var paramNameStart = paramStart + paramTag.Length;
                    var paramNameEnd = parametersSection.IndexOf("\"", paramNameStart);
                    if (paramNameEnd < 0) break;

                    var paramName = parametersSection.Substring(paramNameStart, paramNameEnd - paramNameStart);

                    var paramValueStart = parametersSection.IndexOf(">", paramNameEnd) + 1;
                    var paramValueEnd = parametersSection.IndexOf("</parameter>", paramValueStart);
                    if (paramValueEnd < 0) break;

                    var paramValue = parametersSection.Substring(paramValueStart, paramValueEnd - paramValueStart).Trim();

                    arguments[paramName] = paramValue;
                    _logger.LogInformation("✅ [DEBUG] Extracted parameter: {Name}={Value}", paramName, paramValue);

                    paramIndex = paramValueEnd + "</parameter>".Length;
                }

                // Convert arguments dictionary to JSON
                var argumentsJson = JsonSerializer.Serialize(arguments);

                toolCalls.Add(new LlmToolCall
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = toolName,
                    ArgumentsJson = argumentsJson
                });

                _logger.LogInformation("✅ [DEBUG] Parsed tool call: Name={Name}, Args={Args}", toolName, argumentsJson);

                currentIndex = invokeEnd + invokeEndTag.Length;
            }

            _logger.LogInformation("✅ [DEBUG] Total tool calls parsed: {Count}", toolCalls.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔴 [DEBUG] Failed to parse tool calls from XML");
        }

        return toolCalls;
    }

    // ── Response DTOs ─────────────────────────────────────────────

    private class CustomApiResponse
    {
        [JsonPropertyName("output_text")]
        public string? OutputText { get; set; }

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    private class Usage
    {
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }
}
