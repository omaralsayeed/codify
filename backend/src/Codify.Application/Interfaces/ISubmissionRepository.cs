using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface ISubmissionRepository
{
    Task<IEnumerable<Submission>> GetByProblemAndUserAsync(Guid problemId, Guid? userId);
}
