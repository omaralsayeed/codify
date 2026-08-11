namespace Codify.Domain.Entities;

public class PerformanceProfile
{
    public Guid UserId { get; private set; }
    public string WeakTopicsJson { get; private set; } = "[]";
    public string StrongTopicsJson { get; private set; } = "[]";
    public float SuccessRate { get; private set; }
    public float AverageAttempts { get; private set; }

    /// <summary>
    /// Total AI hints the student has requested across all problems.
    /// Updated every time a hint is persisted via IPerformanceService.
    /// </summary>
    public int TotalHintsUsed { get; private set; }

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
            TotalHintsUsed = 0,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Full recalculation — called after every submission evaluation.
    /// </summary>
    public void Update(
        string weakTopicsJson,
        string strongTopicsJson,
        float successRate,
        float averageAttempts,
        int totalHintsUsed)
    {
        WeakTopicsJson = weakTopicsJson;
        StrongTopicsJson = strongTopicsJson;
        SuccessRate = successRate;
        AverageAttempts = averageAttempts;
        TotalHintsUsed = totalHintsUsed;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Lightweight increment — called immediately after a hint is persisted,
    /// so the count stays in sync without a full recalculation query.
    /// </summary>
    public void IncrementHintCount()
    {
        TotalHintsUsed++;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
