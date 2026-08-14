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
        string? requestText, string? toolsUsedJson, string? reasoningSummary)
    {
        var log = Create(userId, problemId, hintLevel, responseText, requestText);
        log.ToolsUsedJson = toolsUsedJson;
        log.ReasoningSummary = reasoningSummary;
        return log;
    }
}
