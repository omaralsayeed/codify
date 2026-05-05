namespace Codify.Application.DTOs.AI;

public class HintResponse
{
    public string HintText { get; set; } = string.Empty;
    public int HintLevel { get; set; }
    public string? FollowUpQuestion { get; set; }
    public bool HasMoreHints { get; set; }
}
