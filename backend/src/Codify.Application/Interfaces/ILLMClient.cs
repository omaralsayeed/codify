namespace Codify.Application.Interfaces;

public interface ILLMClient
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// One round-trip of the tool-calling loop. Sends the conversation plus
    /// the advertised tool definitions to the model and returns either the tool
    /// calls the model wants executed, or its final text answer. The caller is
    /// responsible for executing any returned tool calls, appending the results
    /// as "tool" messages, and calling this method again.
    /// </summary>
    Task<LlmResponse> CompleteWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Same as CompleteWithToolsAsync but allows overriding the model for this call.
    /// Used for escalation logic (e.g., switching from gpt-4o-mini to gpt-4o for complex cases).
    /// </summary>
    Task<LlmResponse> CompleteWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        string modelOverride,
        CancellationToken cancellationToken = default);
}
