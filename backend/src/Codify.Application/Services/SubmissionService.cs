using Codify.Application.Agents;
using Codify.Application.DTOs.Feedback;
using Codify.Application.DTOs.Submissions;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Codify.Application.Services;

public class SubmissionService(
    ISubmissionRepository submissionRepo,
    IProblemRepository    problemRepo,
    IFeedbackRepository   feedbackRepo,
    ICodeCheckerAgent     codeCheckerAgent,
    ILogger<SubmissionService> logger) : ISubmissionService
{
    public async Task<IEnumerable<SubmissionSummaryResponse>> GetByProblemAsync(
        Guid problemId, Guid userId, bool isInstructor)
    {
        var filterUserId = isInstructor ? (Guid?)null : userId;
        var submissions  = await submissionRepo.GetByProblemAndUserAsync(problemId, filterUserId);
        return submissions.Select(MapToSummary);
    }

    public async Task<SubmissionDetailResponse> CreateAsync(
        CreateSubmissionRequest request, Guid userId)
    {
        var problem = await problemRepo.GetByIdWithTestCasesAsync(request.ProblemId)
            ?? throw new NotFoundException($"Problem {request.ProblemId} not found.");

        // 1. Persist the submission
        var submission = Submission.Create(request.ProblemId, userId, request.Code, request.Language);
        await submissionRepo.AddAsync(submission);
        await submissionRepo.SaveChangesAsync();

        // 2. Trigger the Code Checker Agent asynchronously (fire-and-forget with error guard)
        //    We don't await here so the API responds immediately without waiting for the AI call.
        //    The feedback is stored in the background and retrievable via GET /submissions/{id}/feedback.
        _ = Task.Run(async () =>
        {
            try
            {
                var agentInput = new CodeCheckerAgentInput(
                    SubmissionId:     submission.Id,
                    Code:             submission.Code,
                    Language:         submission.Language.ToString(),
                    ProblemTitle:     problem.Title,
                    ProblemStatement: problem.Statement);

                var feedbackItems = await codeCheckerAgent.AnalyzeAsync(agentInput);

                var records = feedbackItems.Select(item =>
                    FeedbackRecord.Create(submission.Id, item.FeedbackType, item.Message));

                await feedbackRepo.AddRangeAsync(records);
                await feedbackRepo.SaveChangesAsync();

                logger.LogInformation(
                    "CodeChecker saved {Count} feedback records for submission {SubmissionId}.",
                    feedbackItems.Count, submission.Id);
            }
            catch (Exception ex)
            {
                // Never let a background failure affect the student's submission response
                logger.LogError(ex,
                    "CodeChecker background task failed for submission {SubmissionId}.",
                    submission.Id);
            }
        });

        return MapToDetail(submission);
    }

    public async Task<SubmissionDetailResponse> GetByIdAsync(
        Guid submissionId, Guid userId, bool isInstructor)
    {
        var submission = await submissionRepo.GetByIdWithDetailsAsync(submissionId)
            ?? throw new NotFoundException($"Submission {submissionId} not found.");

        if (!isInstructor && submission.UserId != userId)
            throw new ForbiddenException("You do not have access to this submission.");

        return MapToDetail(submission);
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

    private static SubmissionDetailResponse MapToDetail(Submission s) => new()
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
        }).ToList()
    };
}
