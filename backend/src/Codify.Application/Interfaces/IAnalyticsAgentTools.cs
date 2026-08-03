using Codify.Application.Agents;

namespace Codify.Application.Interfaces;

/// <summary>
/// Tools available to the .NET-native Analytics / Tagging Agent. The agent
/// decides which tools to call via OpenAI function calling; these methods only
/// execute the requested operation and return structured data.
/// </summary>
public interface IAnalyticsAgentTools
{
    Task<List<SubmissionSnapshot>> GetSubmissionHistoryAsync(Guid userId);
    Task<TopicPerformanceResult> AggregateTopicPerformanceAsync(Guid userId);
    Task<WeaknessDetectionResult> DetectWeaknessesAsync(Guid userId);
    Task<TrendAnalysisResult> AnalyzeTrendsAsync(Guid userId);
    Task<TagGenerationResult> GenerateTagsAsync(Guid userId);
}

public class TopicPerformanceResult
{
    public List<TopicStat> Topics { get; set; } = [];
}

public class TopicStat
{
    public string Name { get; set; } = string.Empty;
    public int Solved { get; set; }
    public int Failed { get; set; }
    public float SuccessRate { get; set; }
    public float AverageAttempts { get; set; }
    public string AverageDifficulty { get; set; } = string.Empty;
}

public class WeaknessDetectionResult
{
    public List<string> CommonMistakes { get; set; } = [];
    public List<string> WeakTopics { get; set; } = [];
}

public class TrendAnalysisResult
{
    public List<string> ImprovingTopics { get; set; } = [];
    public List<string> DecliningTopics { get; set; } = [];
    public string Consistency { get; set; } = "Low";
    public float LearningVelocity { get; set; }
    public int ActiveDays { get; set; }
    public int InactiveDays { get; set; }
}

public class TagGenerationResult
{
    public string LearningStage { get; set; } = "Beginner";
    public int OverallScore { get; set; }
    public string Consistency { get; set; } = "Low";
    public float Confidence { get; set; }
    public List<string> WeakTopics { get; set; } = [];
    public List<string> StrongTopics { get; set; } = [];
    public string RecommendedDifficulty { get; set; } = "Easy";
}
