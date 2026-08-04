using Codify.Application.DTOs.AI;

namespace Codify.Application.Interfaces;

/// <summary>
/// Orchestrates the Analytics / Tagging Agent: loads submission history,
/// delegates to the agent, upserts the PerformanceProfile, and exposes
/// the structured profile to dashboards and other agents.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>Generate/refresh analytics for the given user and persist the profile.</summary>
    Task<AnalyticsResponse> AnalyzeAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Read the current persisted analytics profile (without re-computing).</summary>
    Task<AnalyticsResponse?> GetAnalyticsAsync(Guid userId, CancellationToken cancellationToken = default);
}
