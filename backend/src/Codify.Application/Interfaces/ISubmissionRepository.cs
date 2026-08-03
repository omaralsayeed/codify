using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface ISubmissionRepository
{
    /// <summary>
    /// Get all submissions for a user with Problem → ProblemTags → ConceptTag
    /// included, for analytics aggregation by topic.
    /// </summary>
    Task<IEnumerable<Submission>> GetAllByUserWithDetailsAsync(Guid userId);

    Task<IEnumerable<Submission>> GetByProblemAndUserAsync(Guid problemId, Guid? userId);
    Task<Submission?> GetByIdWithDetailsAsync(Guid id);
    Task AddAsync(Submission submission);
    Task SaveChangesAsync();
}
