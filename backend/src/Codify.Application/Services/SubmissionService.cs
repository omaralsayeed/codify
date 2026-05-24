using Codify.Application.DTOs.Submissions;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class SubmissionService(
    ISubmissionRepository submissionRepo,
    IProblemRepository problemRepo,
    IUserRepository userRepo,
    IExecutionService executionService,
    IPerformanceService performanceService) : ISubmissionService
{
    public async Task<IEnumerable<SubmissionSummaryResponse>> GetByProblemAsync(
        Guid problemId, Guid userId, bool isInstructor)
    {
        var filterUserId = isInstructor ? (Guid?)null : userId;
        var submissions = await submissionRepo.GetByProblemAndUserAsync(problemId, filterUserId);
        return submissions.Select(MapToSummary);
    }

    public async Task<SubmissionDetailResponse> CreateAsync(CreateSubmissionRequest request, Guid userId)
    {
        // 1. Validate problem exists and load all test cases
        var problem = await problemRepo.GetByIdWithTestCasesAsync(request.ProblemId)
            ?? throw new NotFoundException($"Problem {request.ProblemId} not found.");

        var testCases = problem.TestCases
            .Where(tc => !tc.IsDeleted)
            .OrderBy(tc => tc.OrderIndex)
            .ToList();

        // 2. Persist submission as Pending
        var submission = Submission.Create(request.ProblemId, userId, request.Code, request.Language);
        await submissionRepo.AddAsync(submission);
        await submissionRepo.SaveChangesAsync();

        // 3. Transition to Running
        submission.MarkAsRunning();
        await submissionRepo.SaveChangesAsync();

        // 4. Execute each test case
        int passed = 0;
        int failed = 0;
        int totalExecTimeMs = 0;
        int maxMemoryKb = 0;
        string? firstFailOutput = null;
        string? errorMessage = null;
        SubmissionStatus finalStatus = SubmissionStatus.Accepted;

        foreach (var tc in testCases)
        {
            var result = await executionService.EvaluateAsync(
                request.Code,
                request.Language.ToString(),
                tc.InputData,
                problem.TimeLimitMs,
                problem.MemoryLimitMb);

            totalExecTimeMs += result.ExecutionTimeMs;
            maxMemoryKb = Math.Max(maxMemoryKb, result.MemoryUsedKb);

            // Determine per-test-case outcome
            if (result.CompileError)
            {
                finalStatus = SubmissionStatus.CompileError;
                errorMessage = result.Stderr;
                failed += testCases.Count - passed; // all remaining fail
                break;
            }

            if (result.TimedOut)
            {
                finalStatus = SubmissionStatus.TimeLimitExceeded;
                failed++;
                firstFailOutput ??= $"Input: {tc.InputData}\nExpected: {tc.ExpectedOutput}\nActual: (timed out)";
                continue;
            }

            if (result.RuntimeError)
            {
                finalStatus = SubmissionStatus.RuntimeError;
                errorMessage ??= result.Stderr;
                failed++;
                firstFailOutput ??= $"Input: {tc.InputData}\nExpected: {tc.ExpectedOutput}\nActual: (runtime error)";
                continue;
            }

            var actual = NormalizeOutput(result.ActualOutput);
            var expected = NormalizeOutput(tc.ExpectedOutput);

            if (actual == expected)
            {
                passed++;
            }
            else
            {
                failed++;
                finalStatus = SubmissionStatus.WrongAnswer;
                firstFailOutput ??= $"Input: {tc.InputData}\nExpected: {tc.ExpectedOutput}\nActual: {result.ActualOutput}";
            }
        }

        // 5. Update submission status
        bool isAccepted = finalStatus == SubmissionStatus.Accepted;
        if (isAccepted)
            submission.MarkAsAccepted(totalExecTimeMs, maxMemoryKb, passed, testCases.Count);
        else
            submission.MarkAsFailed(finalStatus, passed, testCases.Count);

        // 6. Persist SubmissionResult (detailed pass/fail breakdown)
        var submissionResult = SubmissionResult.Create(
            submissionId: submission.Id,
            passed: passed,
            failed: failed,
            total: testCases.Count,
            errorMessage: errorMessage,
            outputSummary: firstFailOutput);

        await submissionRepo.AddResultAsync(submissionResult);

        // 7. Update problem-level counters
        problem.IncrementSubmissionCounters(isAccepted);
        await problemRepo.SaveChangesAsync();

        // 8. If first accepted submission, increment user's solved counter
        if (isAccepted)
        {
            var previousAccepted = await submissionRepo.HasPreviousAcceptedAsync(userId, request.ProblemId, submission.Id);
            if (!previousAccepted)
            {
                var user = await userRepo.GetByIdAsync(userId);
                user?.IncrementSolvedProblems();
                await userRepo.SaveChangesAsync();
            }
        }

        // 9. Recalculate performance profile
        await performanceService.UpdateAfterSubmissionAsync(userId);

        // 10. Final save for submission status + result
        await submissionRepo.SaveChangesAsync();

        return await GetByIdAsync(submission.Id, userId, isInstructor: false);
    }

    public async Task<SubmissionDetailResponse> GetByIdAsync(Guid submissionId, Guid userId, bool isInstructor)
    {
        var submission = await submissionRepo.GetByIdWithDetailsAsync(submissionId)
            ?? throw new NotFoundException($"Submission {submissionId} not found.");

        if (!isInstructor && submission.UserId != userId)
            throw new ForbiddenException("You do not have access to this submission.");

        return MapToDetail(submission);
    }

    private static string NormalizeOutput(string output) =>
        output.Trim().Replace("\r\n", "\n").Replace("\r", "\n");

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
        Code = s.Code,
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
