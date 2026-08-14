using Codify.Application.Interfaces;

namespace Codify.Application.Agents;

/// <summary>
/// Tools the agentic Tutor Agent can call via OpenAI function calling. The LLM
/// decides which tools to invoke; these methods only execute a single tool and
/// return structured data. Kept as a contract so implementations are testable.
/// </summary>
public interface ITutorAgentTools
{
    Task<AttemptHistoryResult> GetAttemptHistoryAsync(Guid studentId, Guid problemId);
    Task<List<KnowledgeBaseResult>> SearchKnowledgeBaseAsync(string query, string? conceptTag);
    Task<PartialCodeObservation> CheckPartialCodeAsync(string code, string language);
    Task<List<PreviousHintItem>> GetPreviousHintsAsync(Guid studentId, Guid problemId);
}

/// <summary>How stuck the student is, grounded in real submission/hint history.</summary>
public class AttemptHistoryResult
{
    public int AttemptCount { get; set; }
    public List<string> SubmissionStatuses { get; set; } = [];
    public List<int> PreviousHintLevels { get; set; } = [];
    public List<string> Timestamps { get; set; } = [];
}

/// <summary>Lightweight static observations about a student's partial code.</summary>
public class PartialCodeObservation
{
    public bool SyntaxValid { get; set; } = true;
    public string Structure { get; set; } = string.Empty;
    public List<string> Observations { get; set; } = [];
}

/// <summary>One previously given hint, to avoid repetition.</summary>
public class PreviousHintItem
{
    public int HintLevel { get; set; }
    public string HintText { get; set; } = string.Empty;
    public string GivenAt { get; set; } = string.Empty;
}
