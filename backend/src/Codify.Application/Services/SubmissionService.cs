using Codify.Application.DTOs.Feedback;
using Codify.Application.DTOs.Submissions;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

/// <summary>
/// Handles submission CRUD/queries. The actual Judge0 evaluation (running test cases,
/// computing verdicts, updating counters, triggering the Code Checker Agent) happens
/// off the request thread — see IJudgeEvaluationService and
/// Codify.Infrastructure.BackgroundJobs.SubmissionEvaluationBackgroundService.
/// </summary>
public class SubmissionService(
    ISubmissionRepository submissionRepo,
    IProblemRepository problemRepo,
    IFeedbackRepository feedbackRepo,
    ISubmissionEvaluationQueue evaluationQueue) : ISubmissionService
{
    public async Task<IEnumerable<SubmissionSummaryResponse>> GetByProblemAsync(
        Guid problemId, Guid userId, bool isInstructor)
    {
        var filterUserId = isInstructor ? (Guid?)null : userId;
        var submissions = await submissionRepo.GetByProblemAndUserAsync(problemId, filterUserId);
        return submissions.Select(MapToSummary);
    }

    public async Task<SubmissionDetailResponse> CreateAsync(
        CreateSubmissionRequest request, Guid userId)
    {
        // 1. Validate the problem exists before we accept the submission
        _ = await problemRepo.GetByIdWithTestCasesAsync(request.ProblemId)
            ?? throw new NotFoundException($"Problem {request.ProblemId} not found.");

        // 2. Persist submission as Pending
        var submission = Submission.Create(request.ProblemId, userId, request.Code, request.Language);
        await submissionRepo.AddAsync(submission);
        await submissionRepo.SaveChangesAsync();

        // 3. Hand off to the background evaluation pipeline and return immediately.
        //    JudgeEvaluationService (running inside SubmissionEvaluationBackgroundService)
        //    picks this up, runs every test case through Judge0, and updates the submission's
        //    status asynchronously. The caller polls GET /submissions/{id} for the result.
        evaluationQueue.QueueSubmission(submission.Id);

        return await GetByIdAsync(submission.Id, userId, isInstructor: false);
    }

    public async Task<SubmissionDetailResponse> GetByIdAsync(
        Guid submissionId, Guid userId, bool isInstructor)
    {
        var submission = await submissionRepo.GetByIdWithDetailsAsync(submissionId)
            ?? throw new NotFoundException($"Submission {submissionId} not found.");

        if (!isInstructor && submission.UserId != userId)
            throw new ForbiddenException("You do not have access to this submission.");

        return MapToDetail(submission, isInstructor);
    }

    public async Task<List<FeedbackResponse>> GetFeedbackAsync(
        Guid submissionId, Guid userId, bool isInstructor)
    {
        // Verify submission exists and the caller is allowed to see it
        var submission = await submissionRepo.GetByIdWithDetailsAsync(submissionId)
            ?? throw new NotFoundException($"Submission {submissionId} not found.");

        if (!isInstructor && submission.UserId != userId)
            throw new ForbiddenException("You do not have access to this submission.");

        var records = await feedbackRepo.GetBySubmissionIdAsync(submissionId);

        return records.Select(f => new FeedbackResponse
        {
            Id           = f.Id,
            FeedbackType = f.FeedbackType.ToString(),
            Message      = f.Message,
            Confidence   = f.Confidence,
            CreatedAt    = f.CreatedAt
        }).ToList();
    }

    // -----------------------------------------------------------------------
    // Mappers
    // -----------------------------------------------------------------------

    private static SubmissionSummaryResponse MapToSummary(Submission s) => new()
    {
        SubmissionId    = s.Id,
        Language        = s.Language.ToString(),
        Status          = s.Status.ToString(),
        SubmittedAt     = s.SubmittedAt,
        ExecutionTimeMs = s.ExecutionTimeMs,
        MemoryUsedKb    = s.MemoryUsedKb
    };

    private static SubmissionDetailResponse MapToDetail(Submission s, bool isInstructor) => new()
    {
        SubmissionId    = s.Id,
        ProblemId       = s.ProblemId,
        UserId          = s.UserId,
        Code            = s.Code,
        Language        = s.Language.ToString(),
        Status          = s.Status.ToString(),
        SubmittedAt     = s.SubmittedAt,
        ExecutionTimeMs = s.ExecutionTimeMs,
        MemoryUsedKb    = s.MemoryUsedKb,
        PassedTestCases = s.PassedTestCases,
        TotalTestCases  = s.TotalTestCases,
        Score           = s.Score,
        Result          = s.Result is null ? null : new SubmissionResultDetail
        {
            PassedTestCount = s.Result.PassedTestCount,
            FailedTestCount = s.Result.FailedTestCount,
            TotalTestCount  = s.Result.TotalTestCount,
            ErrorMessage    = s.Result.ErrorMessage,
            OutputSummary   = s.Result.OutputSummary
        },
        AiFeedback = s.FeedbackRecords.Select(f => new FeedbackDetail
        {
            Type    = f.FeedbackType.ToString(),
            Message = f.Message
        }).ToList(),
        TestCaseResults = s.TestCaseResults
            .OrderBy(r => r.OrderIndex)
            .Select(r => new TestCaseResultDetail
            {
                TestCaseId      = r.TestCaseId,
                OrderIndex      = r.OrderIndex,
                IsSample        = r.IsSample,
                Verdict         = r.Verdict.ToString(),
                ExecutionTimeMs = r.ExecutionTimeMs,
                MemoryUsedKb    = r.MemoryUsedKb,
                // Hidden test cases stay hidden from students — same rule TestCaseService applies.
                ActualOutput = (r.IsSample || isInstructor) ? r.ActualOutput : null,
                Stderr       = (r.IsSample || isInstructor) ? r.Stderr : null
            }).ToList()
    };
}
