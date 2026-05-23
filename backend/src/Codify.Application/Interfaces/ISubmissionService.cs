using Codify.Application.DTOs.Submissions;

namespace Codify.Application.Interfaces;

public interface ISubmissionService
{
    Task<IEnumerable<SubmissionSummaryResponse>> GetByProblemAsync(Guid problemId, Guid userId, bool isInstructor);
    Task<SubmissionDetailResponse> CreateAsync(CreateSubmissionRequest request, Guid userId);
    Task<SubmissionDetailResponse> GetByIdAsync(Guid submissionId, Guid userId, bool isInstructor);
}
