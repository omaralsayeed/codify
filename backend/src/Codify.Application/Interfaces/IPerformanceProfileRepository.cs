using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

/// <summary>
/// Repository for the PerformanceProfile entity (1:1 with User).
/// Used by the Analytics Agent to upsert the computed learning profile.
/// </summary>
public interface IPerformanceProfileRepository
{
    Task<PerformanceProfile?> GetByUserIdAsync(Guid userId);
    Task AddAsync(PerformanceProfile profile);
    Task SaveChangesAsync();
}
