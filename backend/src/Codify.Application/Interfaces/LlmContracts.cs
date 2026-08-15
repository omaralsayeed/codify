namespace Codify.Application.Interfaces;

/// <summary>
/// A single message in an LLM conversation. Used by the tool-calling loop:
/// the agent appends assistant messages (with tool calls) and tool-result
/// messages, then resends the whole list to the model.
/// </summary>
public class LlmMessage
{
    /// <summary>One of: "system", "user", "assistant", "tool".</summary>
    public string Role { get; set; } = "user";

    /// <summary>Text content. Empty for assistant messages that only carry tool calls.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Tool calls requested by the assistant (only when Role == "assistant").</summary>
    public List<LlmToolCall> ToolCalls { get; set; } = [];

    /// <summary>The id of the tool call this message responds to (only when Role == "tool").</summary>
    public string? ToolCallId { get; set; }
}

/// <summary>A tool invocation requested by the model.</summary>
public class LlmToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>The raw JSON arguments the model supplied for this tool.</summary>
    public string ArgumentsJson { get; set; } = "{}";
}

/// <summary>A tool definition advertised to the model so it can decide to call it.</summary>
public class LlmToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>The JSON Schema describing the tool's parameters object.</summary>
    public string ParametersJsonSchema { get; set; } = "{}";
}

/// <summary>
/// The result of one LLM round-trip. Either the model requested tool calls
/// (HasToolCalls == true and ToolCalls populated) or it produced a final text
/// answer (FinalText populated).
/// </summary>
public class LlmResponse
{
    public bool HasToolCalls => ToolCalls.Count > 0;
    public List<LlmToolCall> ToolCalls { get; set; } = [];
    public string? FinalText { get; set; }
    public string? ModelUsed { get; set; }
    public int? TotalTokens { get; set; }
}
