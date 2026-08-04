namespace Codify.Domain.Entities;

public class PerformanceProfile
{
    public Guid UserId { get; private set; }
    public string WeakTopicsJson { get; private set; } = "[]";
    public string StrongTopicsJson { get; private set; } = "[]";
    public float SuccessRate { get; private set; }
    public float AverageAttempts { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    // Analytics Agent additions (backward compatible)
    public string LearningStage { get; private set; } = "Beginner";
    public int OverallScore { get; private set; }
    public string Consistency { get; private set; } = "Low";
    public float Confidence { get; private set; }
    public string RecommendedDifficulty { get; private set; } = "Easy";
    /// <summary>Full structured analytics JSON, consumable by APIs and other agents.</summary>
    public string AnalyticsJson { get; private set; } = "{}";

    // Navigation
    public User User { get; private set; } = null!;

    private PerformanceProfile() { }

    public static PerformanceProfile CreateForUser(Guid userId)
    {
        return new PerformanceProfile
        {
            UserId = userId,
            WeakTopicsJson = "[]",
            StrongTopicsJson = "[]",
            SuccessRate = 0f,
            AverageAttempts = 0f,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Legacy update - preserved for backward compatibility.</summary>
    public void Update(string weakTopicsJson, string strongTopicsJson, float successRate, float averageAttempts)
    {
        WeakTopicsJson = weakTopicsJson;
        StrongTopicsJson = strongTopicsJson;
        SuccessRate = successRate;
        AverageAttempts = averageAttempts;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Full analytics update from the Analytics Agent output.</summary>
    public void UpdateAnalytics(
        string weakTopicsJson,
        string strongTopicsJson,
        float successRate,
        float averageAttempts,
        string learningStage,
        int overallScore,
        string consistency,
        float confidence,
        string recommendedDifficulty,
        string analyticsJson)
    {
        WeakTopicsJson = weakTopicsJson;
        StrongTopicsJson = strongTopicsJson;
        SuccessRate = successRate;
        AverageAttempts = averageAttempts;
        LearningStage = learningStage;
        OverallScore = overallScore;
        Consistency = consistency;
        Confidence = confidence;
        RecommendedDifficulty = recommendedDifficulty;
        AnalyticsJson = analyticsJson;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
