using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

/// <summary>
/// Repository for hint history. Used by the agentic Tutor Agent to persist
/// hints with agent metadata and to retrieve previous hints.
/// </summary>
public interface IHintLogRepository
{
    Task<IEnumerable<HintLog>> GetByUserAndProblemAsync(Guid userId, Guid problemId);
    Task AddAsync(HintLog hintLog);
    Task SaveChangesAsync();
}
