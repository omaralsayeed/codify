namespace Codify.Application.Interfaces;

public interface ILLMClient
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a chat completion request with tool definitions. The model may
    /// respond with tool calls (which we execute and resend) or with a final
    /// text response. Returns a LlmResponse that discriminates between the two.
    /// </summary>
    Task<LlmResponse> CompleteWithToolsAsync(
        List<LlmMessage> messages,
        List<LlmToolDefinition> tools,
        CancellationToken cancellationToken = default);
}

/// <summary>A message in the tool-calling conversation.</summary>
public class LlmMessage
{
    public string Role { get; set; } = string.Empty; // "system" | "user" | "assistant" | "tool"
    public string? Content { get; set; }
    public List<LlmToolCall>? ToolCalls { get; set; }
    public string? ToolCallId { get; set; } // set when role == "tool"
}

public class LlmToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = string.Empty;
}

/// <summary>An OpenAI function-calling tool definition.</summary>
public class LlmToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ParametersJsonSchema { get; set; } = string.Empty;
}

/// <summary>
/// The response from CompleteWithToolsAsync — either the model wants to call
/// tools (continue the loop) or it has produced its final text (stop the loop).
/// </summary>
public class LlmResponse
{
    public bool HasToolCalls { get; set; }
    public List<LlmToolCall> ToolCalls { get; set; } = [];
    public string? FinalText { get; set; }
}
