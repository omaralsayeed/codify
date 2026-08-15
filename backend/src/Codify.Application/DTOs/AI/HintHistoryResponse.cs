namespace Codify.Application.DTOs.AI;

public class HintHistoryResponse
{
    public Guid ProblemId { get; set; }
    public int TotalHintsUsed { get; set; }
    public bool CanRequestMore { get; set; }
    public List<HintHistoryItem> Hints { get; set; } = [];
}

public class HintHistoryItem
{
    public int HintLevel { get; set; }
    public string HintText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> ToolsUsed { get; set; } = [];
    public string? ReasoningSummary { get; set; }
    public string? ModelUsed { get; set; }
    public int? TokenCount { get; set; }
    public int? LatencyMs { get; set; }
}
