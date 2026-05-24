using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IPerformanceRepository
{
    Task<PerformanceProfile?> GetByUserIdAsync(Guid userId);
    Task AddAsync(PerformanceProfile profile);
    Task SaveChangesAsync();
}
