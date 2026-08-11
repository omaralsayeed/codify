namespace Codify.Application.Interfaces;

public interface IPerformanceService
{
    /// <summary>
    /// Full recalculation of the PerformanceProfile for a user.
    /// Called after every submission is evaluated.
    /// Recomputes: success rate, average attempts, weak/strong topics, total hints used.
    /// </summary>
    Task UpdateAfterSubmissionAsync(Guid userId);

    /// <summary>
    /// Lightweight update — increments TotalHintsUsed on the profile by 1.
    /// Called immediately after a hint is persisted to keep the count in sync
    /// without running the full recalculation query.
    /// Creates the profile row if it doesn't exist yet.
    /// </summary>
    Task IncrementHintCountAsync(Guid userId);
}
