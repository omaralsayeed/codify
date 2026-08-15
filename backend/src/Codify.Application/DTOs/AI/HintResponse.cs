namespace Codify.Application.DTOs.AI;

public class HintResponse
{
    public string HintText { get; set; } = string.Empty;
    public int HintLevel { get; set; }
    public string? FollowUpQuestion { get; set; }
    public bool HasMoreHints { get; set; }

    /// <summary>Names of the tools the agent called while producing this hint (agentic evidence).</summary>
    public List<string> ToolsUsed { get; set; } = [];

    /// <summary>Short internal note on why the agent chose this hint (not shown as the hint itself).</summary>
    public string? ReasoningSummary { get; set; }

    /// <summary>Which LLM model produced this hint (e.g. "gpt-4o-mini" or "gpt-4o").</summary>
    public string? ModelUsed { get; set; }

    /// <summary>Total tokens consumed across all iterations of the tool-calling loop.</summary>
    public int? TotalTokens { get; set; }

    /// <summary>Total wall-clock time in milliseconds for the full hint generation (all iterations).</summary>
    public int? LatencyMs { get; set; }
}
