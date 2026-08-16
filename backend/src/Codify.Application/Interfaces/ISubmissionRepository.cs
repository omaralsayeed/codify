using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface ISubmissionRepository
{
    Task<IEnumerable<Submission>> GetByProblemAndUserAsync(Guid problemId, Guid? userId);
    Task<Submission?> GetByIdAsync(Guid id);
    Task<Submission?> GetByIdWithDetailsAsync(Guid id);
    /// <summary>Returns all submissions for a user with Problem + ProblemTags + ConceptTag loaded.</summary>
    Task<IEnumerable<Submission>> GetAllByUserAsync(Guid userId);
    /// <summary>Returns true if the user has a previous Accepted submission for this problem (excluding the current one).</summary>
    Task<bool> HasPreviousAcceptedAsync(Guid userId, Guid problemId, Guid excludeSubmissionId);

    /// <summary>Count of submissions created on or after <paramref name="from"/> (UTC). Used for admin stats.</summary>
    Task<int> GetCountFromAsync(DateTime from);

    Task AddAsync(Submission submission);
    Task AddResultAsync(SubmissionResult result);
    Task SaveChangesAsync();
}
