using Codify.Application.DTOs.Submissions;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class SubmissionService(
    ISubmissionRepository submissionRepo,
    IProblemRepository problemRepo) : ISubmissionService
{
    public async Task<IEnumerable<SubmissionSummaryResponse>> GetByProblemAsync(
        Guid problemId, Guid userId, bool isInstructor)
    {
        // Students see only their own; instructors see all
        var filterUserId = isInstructor ? (Guid?)null : userId;
        var submissions = await submissionRepo.GetByProblemAndUserAsync(problemId, filterUserId);

        return submissions.Select(MapToSummary);
    }

    public async Task<SubmissionDetailResponse> CreateAsync(CreateSubmissionRequest request, Guid userId)
    {
        var problem = await problemRepo.GetByIdWithTestCasesAsync(request.ProblemId)
            ?? throw new NotFoundException($"Problem {request.ProblemId} not found.");

        var submission = Submission.Create(request.ProblemId, userId, request.Code, request.Language);
        await submissionRepo.AddAsync(submission);
        await submissionRepo.SaveChangesAsync();

        return MapToDetail(submission);
    }

    public async Task<SubmissionDetailResponse> GetByIdAsync(Guid submissionId, Guid userId, bool isInstructor)
    {
        var submission = await submissionRepo.GetByIdWithDetailsAsync(submissionId)
            ?? throw new NotFoundException($"Submission {submissionId} not found.");

        // Students can only see their own submissions
        if (!isInstructor && submission.UserId != userId)
            throw new ForbiddenException("You do not have access to this submission.");

        return MapToDetail(submission);
    }

    private static SubmissionSummaryResponse MapToSummary(Submission s) => new()
    {
        SubmissionId = s.Id,
        Language = s.Language.ToString(),
        Status = s.Status.ToString(),
        SubmittedAt = s.SubmittedAt,
        ExecutionTimeMs = s.ExecutionTimeMs,
        MemoryUsedKb = s.MemoryUsedKb
    };

    private static SubmissionDetailResponse MapToDetail(Submission s) => new()
    {
        SubmissionId = s.Id,
        ProblemId = s.ProblemId,
        UserId = s.UserId,
        Language = s.Language.ToString(),
        Status = s.Status.ToString(),
        SubmittedAt = s.SubmittedAt,
        ExecutionTimeMs = s.ExecutionTimeMs,
        MemoryUsedKb = s.MemoryUsedKb,
        PassedTestCases = s.PassedTestCases,
        TotalTestCases = s.TotalTestCases,
        Score = s.Score,
        Result = s.Result is null ? null : new SubmissionResultDetail
        {
            PassedTestCount = s.Result.PassedTestCount,
            FailedTestCount = s.Result.FailedTestCount,
            TotalTestCount = s.Result.TotalTestCount,
            ErrorMessage = s.Result.ErrorMessage,
            OutputSummary = s.Result.OutputSummary
        },
        AiFeedback = s.FeedbackRecords.Select(f => new FeedbackDetail
        {
            Type = f.FeedbackType.ToString(),
            Message = f.Message
        }).ToList()
    };
}
