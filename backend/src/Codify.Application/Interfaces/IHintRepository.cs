using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IHintRepository
{
    /// <summary>Returns the current max hint level for this user+problem pair, or 0 if none.</summary>
    Task<int> GetCurrentHintLevelAsync(Guid userId, Guid problemId);

    /// <summary>Returns all hint logs for this user+problem, ordered by hint level ascending.</summary>
    Task<IEnumerable<HintLog>> GetByUserAndProblemAsync(Guid userId, Guid problemId);

    Task AddAsync(HintLog hintLog);
    Task SaveChangesAsync();
}
