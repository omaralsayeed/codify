using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

// Think of this as a "contract" — the Application layer says "I need these methods",
// and the Infrastructure layer (repository) fulfills the contract using EF Core + SQL.
public interface IConceptTagRepository
{
    Task<IEnumerable<ConceptTag>> GetAllAsync();
    Task<IEnumerable<ConceptTag>> GetByIdsAsync(IEnumerable<Guid> ids);

    // New methods for full CRUD:
    Task<ConceptTag?> GetByIdAsync(Guid id);
    Task<ConceptTag?> GetByNameAsync(string name); // Used to prevent duplicate tag names

    /// <summary>
    /// Returns an existing tag matching the name (case-insensitive), or creates and persists
    /// a new one. Used when admin creates/edits a problem with tag names instead of IDs.
    /// </summary>
    Task<ConceptTag> GetOrCreateByNameAsync(string name);

    Task AddAsync(ConceptTag tag);
    Task SaveChangesAsync();

    // ProblemTag operations — managing the join table
    Task<IEnumerable<ProblemTag>> GetTagsByProblemIdAsync(Guid problemId);
    Task<ProblemTag?> GetProblemTagAsync(Guid problemId, Guid conceptTagId);
    Task AddProblemTagAsync(ProblemTag problemTag);
    void RemoveProblemTag(ProblemTag problemTag);
}
