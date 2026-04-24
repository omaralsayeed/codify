using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface ISubmissionRepository
{
    Task<IEnumerable<Submission>> GetByProblemAndUserAsync(Guid problemId, Guid? userId);
    Task<Submission?> GetByIdWithDetailsAsync(Guid id);
    Task AddAsync(Submission submission);
    Task SaveChangesAsync();
}
