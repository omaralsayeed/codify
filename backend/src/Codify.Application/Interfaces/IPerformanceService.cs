namespace Codify.Application.Interfaces;

public interface IPerformanceService
{
    /// <summary>
    /// Recalculates and persists the PerformanceProfile for a user
    /// after a submission has been evaluated.
    /// </summary>
    Task UpdateAfterSubmissionAsync(Guid userId);
}
