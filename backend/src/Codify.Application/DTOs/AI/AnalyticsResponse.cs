namespace Codify.Application.DTOs.AI;

/// <summary>
/// Structured analytics response, consumable by dashboards, the Tutor Agent,
/// the Code Analysis Agent, and future adaptive learning features.
/// </summary>
public class AnalyticsResponse
{
    public Guid UserId { get; set; }
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
    public List<AnalyticsPracticePlanItem> PracticePlan { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
    public float SuccessRate { get; set; }
    public float AverageAttempts { get; set; }
    public List<string> ToolsUsed { get; set; } = [];
    public string ReasoningSummary { get; set; } = string.Empty;
    public DateTime? LastUpdatedAt { get; set; }
}

public class AnalyticsPracticePlanItem
{
    public string Topic { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int Priority { get; set; }
}
