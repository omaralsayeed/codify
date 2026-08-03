namespace Codify.Application.Interfaces;

/// <summary>
/// The four tools the agentic Tutor Agent can call via OpenAI function calling.
/// The LLM decides which tools to call, in what order, and how many — our code
/// only executes whatever the model requests.
/// </summary>
public interface ITutorAgentTools
{
    /// <summary>Tool 1: get_attempt_history — how stuck is the student really?</summary>
    Task<AttemptHistoryResult> GetAttemptHistoryAsync(Guid studentId, Guid problemId);

    /// <summary>Tool 2: search_knowledge_base — retrieve concept-level grounding.</summary>
    Task<List<KnowledgeBaseResult>> SearchKnowledgeBaseAsync(string query, string? conceptTag);

    /// <summary>Tool 3: check_partial_code — lightweight static observations.</summary>
    Task<PartialCodeObservation> CheckPartialCodeAsync(string code, string language);

    /// <summary>Tool 4: get_previous_hints — avoid repeating guidance.</summary>
    Task<List<PreviousHintItem>> GetPreviousHintsAsync(Guid studentId, Guid problemId);
}

public class AttemptHistoryResult
{
    public int AttemptCount { get; set; }
    public List<string> SubmissionStatuses { get; set; } = [];
    public List<int> PreviousHintLevels { get; set; } = [];
    public List<string> Timestamps { get; set; } = [];
}

public class PartialCodeObservation
{
    public bool SyntaxValid { get; set; }
    public string Structure { get; set; } = string.Empty;
    public List<string> Observations { get; set; } = [];
}

public class PreviousHintItem
{
    public int HintLevel { get; set; }
    public string HintText { get; set; } = string.Empty;
    public string GivenAt { get; set; } = string.Empty;
}
