using Codify.Application.DTOs.Submissions;
using Codify.Application.Interfaces;

namespace Codify.Application.Services;

public class SubmissionService(ISubmissionRepository submissionRepo) : ISubmissionService
{
    public async Task<IEnumerable<SubmissionSummaryResponse>> GetByProblemAsync(
        Guid problemId, Guid userId, bool isInstructor)
    {
        // Students see only their own; instructors see all
        var filterUserId = isInstructor ? (Guid?)null : userId;
        var submissions = await submissionRepo.GetByProblemAndUserAsync(problemId, filterUserId);

        return submissions.Select(s => new SubmissionSummaryResponse
        {
            SubmissionId = s.Id,
            Language = s.Language.ToString(),
            Status = s.Status.ToString(),
            SubmittedAt = s.SubmittedAt,
            ExecutionTimeMs = s.ExecutionTimeMs,
            MemoryUsedKb = s.MemoryUsedKb
        });
    }
}
