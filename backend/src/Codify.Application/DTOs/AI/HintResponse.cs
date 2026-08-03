namespace Codify.Application.DTOs.AI;

public class HintResponse
{
    public string HintText { get; set; } = string.Empty;
    public int HintLevel { get; set; }
    public string? FollowUpQuestion { get; set; }
    public bool HasMoreHints { get; set; }

    // Agent metadata — evidence of the agent's tool-calling decisions.
    public List<string> ToolsUsed { get; set; } = [];
    public string? ReasoningSummary { get; set; }
}
