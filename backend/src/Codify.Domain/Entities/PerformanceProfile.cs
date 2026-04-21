namespace Codify.Domain.Entities;

public class PerformanceProfile
{
    public Guid UserId { get; private set; }
    public string WeakTopicsJson { get; private set; } = "[]";
    public string StrongTopicsJson { get; private set; } = "[]";
    public float SuccessRate { get; private set; }
    public float AverageAttempts { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

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

    public void Update(string weakTopicsJson, string strongTopicsJson, float successRate, float averageAttempts)
    {
        WeakTopicsJson = weakTopicsJson;
        StrongTopicsJson = strongTopicsJson;
        SuccessRate = successRate;
        AverageAttempts = averageAttempts;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
