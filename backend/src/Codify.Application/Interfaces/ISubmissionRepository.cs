using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface ISubmissionRepository
{
    Task<IEnumerable<Submission>> GetByProblemAndUserAsync(Guid problemId, Guid? userId);
    Task<Submission?> GetByIdWithDetailsAsync(Guid id);
    /// <summary>Returns all submissions for a user with Problem + ProblemTags + ConceptTag loaded.</summary>
    Task<IEnumerable<Submission>> GetAllByUserAsync(Guid userId);
    Task AddAsync(Submission submission);
    Task SaveChangesAsync();
}
