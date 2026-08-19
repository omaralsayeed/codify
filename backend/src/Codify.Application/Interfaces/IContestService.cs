using Codify.Application.DTOs.Contests;

namespace Codify.Application.Interfaces;

public interface IContestService
{
    Task<List<ContestDto>> GetInstructorContestsAsync(Guid instructorId);
    Task<ContestDto> GetContestByIdAsync(Guid contestId);
    Task<ContestDto> CreateContestAsync(Guid instructorId, CreateContestRequest request);
    Task<List<ContestResultDto>> GetContestResultsAsync(Guid contestId);
    Task<List<ContestResultDto>> GetStudentContestHistoryAsync(Guid studentId);
    Task<StudentContestsOverviewDto> GetStudentContestsOverviewAsync(Guid studentId);
    Task RespondToInvitationAsync(Guid contestId, Guid studentId, bool accept);
    Task<List<StudentCandidateDto>> SearchStudentCandidatesAsync(Guid instructorId, string? query);
}
