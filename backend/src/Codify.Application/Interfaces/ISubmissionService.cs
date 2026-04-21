using Codify.Application.DTOs.Submissions;

namespace Codify.Application.Interfaces;

public interface ISubmissionService
{
    Task<IEnumerable<SubmissionSummaryResponse>> GetByProblemAsync(Guid problemId, Guid userId, bool isInstructor);
}
