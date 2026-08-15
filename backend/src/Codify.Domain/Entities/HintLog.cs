namespace Codify.Domain.Entities;

public class HintLog
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ProblemId { get; private set; }
    public int HintLevel { get; private set; }
    public string? RequestText { get; private set; }
    public string ResponseText { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// JSON array of the tool names the agentic Tutor Agent called while
    /// producing this hint. Evidence of agentic decision-making.
    /// </summary>
    public string? ToolsUsedJson { get; private set; }

    /// <summary>Short internal note on why the agent chose this hint.</summary>
    public string? ReasoningSummary { get; private set; }

    /// <summary>Which LLM model produced this hint (e.g. "gpt-4o-mini" or "gpt-4o").</summary>
    public string? ModelUsed { get; private set; }

    /// <summary>Total tokens consumed across all iterations of the tool-calling loop.</summary>
    public int? TokenCount { get; private set; }

    /// <summary>Total wall-clock time in milliseconds for the full hint generation (all iterations).</summary>
    public int? LatencyMs { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;
    public Problem Problem { get; private set; } = null!;

    private HintLog() { }

    public static HintLog Create(Guid userId, Guid problemId, int hintLevel, string responseText, string? requestText = null)
    {
        return new HintLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProblemId = problemId,
            HintLevel = hintLevel,
            ResponseText = responseText,
            RequestText = requestText,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a hint log that also records the agentic Tutor Agent's
    /// decision-making evidence (tools used + reasoning summary).
    /// </summary>
    public static HintLog CreateWithAgentMetadata(
        Guid userId, Guid problemId, int hintLevel, string responseText,
        string? requestText, string? toolsUsedJson, string? reasoningSummary,
        string? modelUsed = null, int? tokenCount = null, int? latencyMs = null)
    {
        var log = Create(userId, problemId, hintLevel, responseText, requestText);
        log.ToolsUsedJson = toolsUsedJson;
        log.ReasoningSummary = reasoningSummary;
        log.ModelUsed = modelUsed;
        log.TokenCount = tokenCount;
        log.LatencyMs = latencyMs;
        return log;
    }
}
