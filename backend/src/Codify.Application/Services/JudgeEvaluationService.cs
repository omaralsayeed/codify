using Codify.Application.Agents;
using Codify.Application.DTOs.Execution;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;
using Microsoft.Extensions.Logging;
// Codify.Application.DTOs.Execution.TestCaseResult (legacy quick-run DTO) and
// Codify.Domain.Entities.TestCaseResult (our new per-submission verdict entity) share a
// name — alias the domain entity here to keep every reference in this file unambiguous.
using TestCaseResultEntity = Codify.Domain.Entities.TestCaseResult;

namespace Codify.Application.Services;

/// <summary>
/// The core evaluation pipeline for one submission. Dequeued and invoked by the
/// background worker (Codify.Infrastructure.BackgroundJobs.SubmissionEvaluationBackgroundService)
/// on its own DI scope, so it never blocks the HTTP request that created the submission.
/// </summary>
public class JudgeEvaluationService(
    ISubmissionRepository submissionRepo,
    IProblemRepository problemRepo,
    IUserRepository userRepo,
    IExecutionService executionService,
    ITaggingService taggingService,
    ITestCaseResultRepository testCaseResultRepo,
    IFeedbackRepository feedbackRepo,
    ICodeCheckerAgent codeCheckerAgent,
    ILogger<JudgeEvaluationService> logger) : IJudgeEvaluationService
{
    public async Task EvaluateSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        var submission = await submissionRepo.GetByIdAsync(submissionId)
            ?? throw new NotFoundException($"Submission {submissionId} not found.");

        var problem = await problemRepo.GetByIdWithTestCasesAsync(submission.ProblemId)
            ?? throw new NotFoundException($"Problem {submission.ProblemId} not found.");

        var testCases = problem.TestCases
            .Where(tc => !tc.IsDeleted)
            .OrderBy(tc => tc.OrderIndex)
            .ToList();

        // 1. Transition to Running
        submission.MarkAsRunning();
        await submissionRepo.SaveChangesAsync();

        // 2. Execute every test case (public + hidden) against Judge0
        var testCaseResults = new List<TestCaseResultEntity>();
        int passed = 0;
        int failed = 0;
        int totalExecTimeMs = 0;
        int maxMemoryKb = 0;
        string? firstFailOutput = null;
        string? errorMessage = null;
        SubmissionStatus finalStatus = SubmissionStatus.Accepted;

        foreach (var tc in testCases)
        {
            var eval = await executionService.EvaluateAsync(
                submission.Code,
                submission.Language.ToString(),
                tc.InputData,
                problem.TimeLimitMs,
                problem.MemoryLimitMb,
                cancellationToken);

            totalExecTimeMs += eval.ExecutionTimeMs;
            maxMemoryKb = Math.Max(maxMemoryKb, eval.MemoryUsedKb);

            var verdict = DetermineVerdict(eval, tc.ExpectedOutput, problem.MemoryLimitMb);

            testCaseResults.Add(TestCaseResultEntity.Create(
                submissionId: submission.Id,
                testCaseId: tc.Id,
                verdict: verdict,
                actualOutput: eval.ActualOutput,
                stderr: string.IsNullOrWhiteSpace(eval.Stderr) ? null : eval.Stderr,
                executionTimeMs: eval.ExecutionTimeMs,
                memoryUsedKb: eval.MemoryUsedKb,
                isSample: tc.IsSample,
                orderIndex: tc.OrderIndex));

            if (verdict == SubmissionStatus.Accepted)
            {
                passed++;
                continue;
            }

            failed++;

            // The submission's overall status takes the first non-Accepted verdict encountered,
            // matching the original synchronous pipeline's precedence (compile error short-circuits,
            // otherwise first failure reason wins).
            if (finalStatus == SubmissionStatus.Accepted)
                finalStatus = verdict;

            if (verdict == SubmissionStatus.CompileError)
                errorMessage = eval.Stderr;
            else if (verdict == SubmissionStatus.RuntimeError)
                errorMessage ??= eval.Stderr;

            firstFailOutput ??= verdict switch
            {
                SubmissionStatus.TimeLimitExceeded => $"Input: {tc.InputData}\nExpected: {tc.ExpectedOutput}\nActual: (timed out)",
                SubmissionStatus.RuntimeError => $"Input: {tc.InputData}\nExpected: {tc.ExpectedOutput}\nActual: (runtime error)",
                SubmissionStatus.MemoryLimitExceeded => $"Input: {tc.InputData}\nExpected: {tc.ExpectedOutput}\nActual: (memory limit exceeded)",
                _ => $"Input: {tc.InputData}\nExpected: {tc.ExpectedOutput}\nActual: {eval.ActualOutput}"
            };

            // Compile errors apply to every test case identically — no point burning
            // Judge0 calls on the remaining ones.
            if (verdict == SubmissionStatus.CompileError)
                break;
        }

        // 3. Persist per-test-case results
        await testCaseResultRepo.AddRangeAsync(testCaseResults);
        await testCaseResultRepo.SaveChangesAsync();

        // 4. Update submission status
        bool isAccepted = finalStatus == SubmissionStatus.Accepted;
        if (isAccepted)
            submission.MarkAsAccepted(totalExecTimeMs, maxMemoryKb, passed, testCases.Count);
        else
            submission.MarkAsFailed(finalStatus, passed, testCases.Count);

        // 5. Persist SubmissionResult (aggregate pass/fail breakdown)
        var submissionResult = SubmissionResult.Create(
            submissionId: submission.Id,
            passed: passed,
            failed: failed,
            total: testCases.Count,
            errorMessage: errorMessage,
            outputSummary: firstFailOutput);

        await submissionRepo.AddResultAsync(submissionResult);

        // 6. Update problem-level counters
        problem.IncrementSubmissionCounters(isAccepted);
        await problemRepo.SaveChangesAsync();

        // 7. If first accepted submission, increment user's solved counter
        if (isAccepted)
        {
            var previousAccepted = await submissionRepo.HasPreviousAcceptedAsync(
                submission.UserId, submission.ProblemId, submission.Id);
            if (!previousAccepted)
            {
                var user = await userRepo.GetByIdAsync(submission.UserId);
                user?.IncrementSolvedProblems();
                await userRepo.SaveChangesAsync();
            }
        }

        // 8. Fire the Tagging Agent's on-submission hook: tag the current problem
        //    if untagged, scan all other untagged problems, and refresh the student's
        //    concept-tag profile (weak/strong topics).
        await taggingService.TagOnSubmissionAsync(submission.ProblemId, submission.UserId, cancellationToken);

        // 9. Final save for submission status + result
        await submissionRepo.SaveChangesAsync();

        // 10. Run the Code Checker Agent. We're already off the request thread (this whole
        //     method runs inside the background worker), so we await it directly instead of
        //     spawning a second fire-and-forget task — but a failure here must never take
        //     down the evaluation that already succeeded.
        await RunCodeCheckerAgentAsync(submission, problem, cancellationToken);
    }

    // ── Private ───────────────────────────────────────────────────

    /// <summary>
    /// Maps a raw Judge0 execution result + expected output into a per-test-case verdict.
    /// Codify does its own stdout comparison (Judge0's own "Wrong Answer" status isn't used,
    /// since we never send expected_output to Judge0 — see ExecutionService.EvaluateAsync).
    /// </summary>
    private static SubmissionStatus DetermineVerdict(
        TestCaseExecutionResult eval, string expectedOutput, int memoryLimitMb)
    {
        if (eval.CompileError)
            return SubmissionStatus.CompileError;

        if (eval.TimedOut)
            return SubmissionStatus.TimeLimitExceeded;

        if (eval.RuntimeError)
            return SubmissionStatus.RuntimeError;

        if (eval.MemoryUsedKb > memoryLimitMb * 1024)
            return SubmissionStatus.MemoryLimitExceeded;

        var actual = NormalizeOutput(eval.ActualOutput);
        var expected = NormalizeOutput(expectedOutput);

        return actual == expected ? SubmissionStatus.Accepted : SubmissionStatus.WrongAnswer;
    }

    private async Task RunCodeCheckerAgentAsync(Submission submission, Problem problem, CancellationToken cancellationToken)
    {
        try
        {
            var agentInput = new CodeCheckerAgentInput(
                SubmissionId: submission.Id,
                Code: submission.Code,
                Language: submission.Language.ToString(),
                ProblemTitle: problem.Title,
                ProblemStatement: problem.Statement);

            var feedbackItems = await codeCheckerAgent.AnalyzeAsync(agentInput, cancellationToken);

            var records = feedbackItems.Select(item =>
                FeedbackRecord.Create(submission.Id, item.FeedbackType, item.Message, item.Confidence));

            await feedbackRepo.AddRangeAsync(records);
            await feedbackRepo.SaveChangesAsync();

            logger.LogInformation(
                "CodeChecker saved {Count} feedback records for submission {SubmissionId}.",
                feedbackItems.Count, submission.Id);
        }
        catch (Exception ex)
        {
            // Never let a Code Checker failure affect an already-evaluated submission.
            logger.LogError(ex,
                "CodeChecker background task failed for submission {SubmissionId}.",
                submission.Id);
        }
    }

    private static string NormalizeOutput(string output) =>
        output.Trim().Replace("\r\n", "\n").Replace("\r", "\n");
}
