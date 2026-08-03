namespace Codify.Application.Agents;

/// <summary>
/// Contract for the Analytics / Tagging Agent. Implemented as a .NET-native
/// agentic service that uses OpenAI function calling to decide which tools to invoke.
/// The Application layer depends only on this interface, never on transport details.
/// </summary>
public interface IAnalyticsAgent
{
    Task<AnalyticsResult> AnalyzeAsync(AnalyticsAgentInput input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Input payload for the Analytics Agent. The service layer assembles this
/// from the user and available topics before delegating to the agent.
/// The agent itself decides which tools to call to fetch and analyze data.
/// </summary>
public class AnalyticsAgentInput
{
    public Guid UserId { get; set; }
    public List<string> AvailableTopics { get; set; } = [];
    public int HintCount { get; set; }
}

/// <summary>A flattened submission record used by analytics tools.</summary>
public class SubmissionSnapshot
{
    public Guid SubmissionId { get; set; }
    public Guid ProblemId { get; set; }
    public string ProblemTitle { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<string> Topics { get; set; } = [];
    public string Language { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int? ExecutionTimeMs { get; set; }
    public int? MemoryUsedKb { get; set; }
    public int PassedTestCases { get; set; }
    public int TotalTestCases { get; set; }
    public decimal? Score { get; set; }
}

/// <summary>A flattened feedback record used by analytics tools.</summary>
public class FeedbackSnapshot
{
    public string FeedbackType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Structured analytics result from the agent.</summary>
public class AnalyticsResult
{
    public string LearningStage { get; set; } = "Beginner";
    public int OverallScore { get; set; }
    public string Consistency { get; set; } = "Low";
    public float Confidence { get; set; }
    public List<string> WeakTopics { get; set; } = [];
    public List<string> StrongTopics { get; set; } = [];
    public List<string> ImprovingTopics { get; set; } = [];
    public List<string> DecliningTopics { get; set; } = [];
    public List<string> CommonMistakes { get; set; } = [];
    public List<string> RecommendedTopics { get; set; } = [];
    public string RecommendedProblemDifficulty { get; set; } = "Easy";
    public List<PracticePlanItem> PracticePlan { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
    public float SuccessRate { get; set; }
    public float AverageAttempts { get; set; }
    public List<string> ToolsUsed { get; set; } = [];
    public string ReasoningSummary { get; set; } = string.Empty;
}

public class PracticePlanItem
{
    public string Topic { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int Priority { get; set; }
}
