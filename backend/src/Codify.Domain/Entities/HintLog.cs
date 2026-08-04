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

    // Agent metadata — populated by the agentic Tutor Agent to evidence
    // which tools the LLM chose to call and its own reasoning.
    public string? ToolsUsedJson { get; private set; }
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
    /// Create a HintLog with agentic metadata (tools used + reasoning summary)
    /// so we can demonstrate the agent's decision-making during evaluation.
    /// </summary>
    public static HintLog CreateWithAgentMetadata(
        Guid userId,
        Guid problemId,
        int hintLevel,
        string responseText,
        string? requestText,
        string toolsUsedJson,
        string reasoningSummary)
    {
        return new HintLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProblemId = problemId,
            HintLevel = hintLevel,
            ResponseText = responseText,
            RequestText = requestText,
            ToolsUsedJson = toolsUsedJson,
            ReasoningSummary = reasoningSummary,
            CreatedAt = DateTime.UtcNow
        };
    }
}