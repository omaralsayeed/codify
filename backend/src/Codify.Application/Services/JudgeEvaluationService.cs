using Codify.Application.Agents;
using Codify.Application.DTOs.Execution;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;
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
    IServiceScopeFactory scopeFactory,
    ILogger<JudgeEvaluationService> logger) : IJudgeEvaluationService
{
    public async Task EvaluateSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("🎬 [EVAL START] Submission {SubmissionId} - Beginning evaluation...", submissionId);
        
        var submission = await submissionRepo.GetByIdAsync(submissionId)
            ?? throw new NotFoundException($"Submission {submissionId} not found.");

        logger.LogInformation("✅ [EVAL] Submission loaded: User={UserId}, Problem={ProblemId}, Language={Language}", 
            submission.UserId, submission.ProblemId, submission.Language);

        var problem = await problemRepo.GetByIdWithTestCasesAsync(submission.ProblemId)
            ?? throw new NotFoundException($"Problem {submission.ProblemId} not found.");

        var testCases = problem.TestCases
            .Where(tc => !tc.IsDeleted)
            .OrderBy(tc => tc.OrderIndex)
            .ToList();

        logger.LogInformation("✅ [EVAL] Problem loaded: Title=\"{Title}\", TestCases={Count} (Samples={Samples}, Hidden={Hidden})", 
            problem.Title, testCases.Count, 
            testCases.Count(tc => tc.IsSample), 
            testCases.Count(tc => !tc.IsSample));

        // 1. Transition to Running
        submission.MarkAsRunning();
        await submissionRepo.SaveChangesAsync();
        logger.LogInformation("🏃 [EVAL] Submission marked as Running");

        // 2. Execute every test case (public + hidden) against Judge0
        var testCaseResults = new List<TestCaseResultEntity>();
        int passed = 0;
        int failed = 0;
        int totalExecTimeMs = 0;
        int maxMemoryKb = 0;
        string? firstFailOutput = null;
        string? errorMessage = null;
        SubmissionStatus finalStatus = SubmissionStatus.Accepted;

        logger.LogInformation("🔄 [EVAL] Starting test case execution (total: {Count})...", testCases.Count);

        // Use batch evaluation for significant speedup (1-2 Judge0 round-trips instead of N)
        logger.LogInformation("🚀 [EVAL] Using BATCH evaluation for all test cases");
        var batchResults = await executionService.EvaluateBatchAsync(
            submission.Code,
            submission.Language.ToString(),
            testCases,
            problem.TimeLimitMs,
            problem.MemoryLimitMb,
            problem,
            cancellationToken);

        for (var i = 0; i < testCases.Count; i++)
        {
            var tc = testCases[i];
            var eval = batchResults[i];

            logger.LogInformation("✅ [TEST] Test case {Index} executed: ExecTime={ExecTime}ms, Memory={Memory}KB, CompileError={CompileError}, RuntimeError={RuntimeError}, TimedOut={TimedOut}", 
                tc.OrderIndex + 1, eval.ExecutionTimeMs, eval.MemoryUsedKb, eval.CompileError, eval.RuntimeError, eval.TimedOut);

            totalExecTimeMs += eval.ExecutionTimeMs;
            maxMemoryKb = Math.Max(maxMemoryKb, eval.MemoryUsedKb);

            var verdict = DetermineVerdict(eval, tc.ExpectedOutput, problem.MemoryLimitMb);
            
            logger.LogInformation("⚖️  [TEST] Test case {Index} verdict: {Verdict}", tc.OrderIndex + 1, verdict);

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
                logger.LogInformation("✅ [TEST] Test case {Index} PASSED", tc.OrderIndex + 1);
                continue;
            }

            failed++;
            logger.LogInformation("❌ [TEST] Test case {Index} FAILED: {Verdict}", tc.OrderIndex + 1, verdict);

            // The submission's overall status takes the first non-Accepted verdict encountered
            if (finalStatus == SubmissionStatus.Accepted)
            {
                finalStatus = verdict;
                logger.LogInformation("🎯 [EVAL] Final status set to: {Status}", finalStatus);
            }

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

            // Compile errors apply to every test case identically — still return all results
            // but we can optimize by not polling the rest (batch already executed all)
            if (verdict == SubmissionStatus.CompileError)
            {
                logger.LogWarning("⚠️  [EVAL] Compile error detected. Results for remaining test cases are also compile errors.");
                break;
            }
        }

        logger.LogInformation("📊 [EVAL] All tests completed: Passed={Passed}, Failed={Failed}, TotalTime={TotalTime}ms, MaxMemory={MaxMemory}KB, FinalStatus={Status}", 
            passed, failed, totalExecTimeMs, maxMemoryKb, finalStatus);

        // 3. Persist per-test-case results
        logger.LogInformation("💾 [EVAL] Persisting {Count} test case results...", testCaseResults.Count);
        await testCaseResultRepo.AddRangeAsync(testCaseResults);
        await testCaseResultRepo.SaveChangesAsync();
        logger.LogInformation("✅ [EVAL] Test case results saved");

        // 4. Update submission status
        bool isAccepted = finalStatus == SubmissionStatus.Accepted;
        if (isAccepted)
        {
            submission.MarkAsAccepted(totalExecTimeMs, maxMemoryKb, passed, testCases.Count);
            logger.LogInformation("🎉 [EVAL] Submission marked as ACCEPTED");
        }
        else
        {
            submission.MarkAsFailed(finalStatus, passed, testCases.Count);
            logger.LogInformation("❌ [EVAL] Submission marked as FAILED: {Status}", finalStatus);
        }

        // 5. Persist SubmissionResult (aggregate pass/fail breakdown)
        var submissionResult = SubmissionResult.Create(
            submissionId: submission.Id,
            passed: passed,
            failed: failed,
            total: testCases.Count,
            errorMessage: errorMessage,
            outputSummary: firstFailOutput);

        await submissionRepo.AddResultAsync(submissionResult);
        logger.LogInformation("💾 [EVAL] Submission result saved");

        // 6. Update problem-level counters
        problem.IncrementSubmissionCounters(isAccepted);
        await problemRepo.SaveChangesAsync();
        logger.LogInformation("📊 [EVAL] Problem counters updated");

        // 7. If first accepted submission, increment user's solved counter
        if (isAccepted)
        {
            var previousAccepted = await submissionRepo.HasPreviousAcceptedAsync(
                submission.UserId, submission.ProblemId, submission.Id);
            if (!previousAccepted)
            {
                logger.LogInformation("🏆 [EVAL] First accepted submission for this user/problem! Incrementing solved count...");
                var user = await userRepo.GetByIdAsync(submission.UserId);
                user?.IncrementSolvedProblems();
                await userRepo.SaveChangesAsync();
                logger.LogInformation("✅ [EVAL] User solved count incremented");
            }
            else
            {
                logger.LogInformation("ℹ️  [EVAL] User has previous accepted submission for this problem");
            }
        }

        // 8. Fire the Tagging Agent's on-submission hook: tag the current problem
        //    if untagged, scan all other untagged problems, and refresh the student's
        //    concept-tag profile (weak/strong topics).
        logger.LogInformation("🏷️  [EVAL] Running tagging agent...");
        await taggingService.TagOnSubmissionAsync(submission.ProblemId, submission.UserId, cancellationToken);
        logger.LogInformation("✅ [EVAL] Tagging completed");

        // 9. Final save for submission status + result BEFORE running the Code Checker Agent
        //     This ensures users can see their results immediately via the API
        await submissionRepo.SaveChangesAsync();
        logger.LogInformation("💾 [EVAL] Final submission state saved - results are now available to user");

        // 10. Run the Code Checker Agent. We're already off the request thread (this whole
        //     method runs inside the background worker), so we can await it without blocking
        //     the HTTP response. The submission results are already saved, so users see their
        //     verdict immediately. AI feedback appears a few seconds later when they refresh.
        //     
        //     IMPORTANT: We create a NEW DI scope here so the Code Checker gets its own fresh
        //     DbContext. The parent scope's DbContext is disposed when this method finishes,
        //     so we can't reuse the injected repositories for async operations after SaveChanges.
        logger.LogInformation("🤖 [EVAL] Starting Code Checker Agent (submission results already saved)...");
        await RunCodeCheckerAgentAsync(submission.Id, submission.Code, submission.Language.ToString(), 
            problem.Title, problem.Statement, cancellationToken);
        
        logger.LogInformation("🏁 [EVAL COMPLETE] Submission {SubmissionId} evaluation finished successfully", submissionId);
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

    private async Task RunCodeCheckerAgentAsync(Guid submissionId, string code, string language, 
        string problemTitle, string problemStatement, CancellationToken cancellationToken)
    {
        try
        {
            // Create a NEW scope so we get a fresh DbContext that won't be disposed
            // when the parent EvaluateSubmissionAsync method finishes
            using var scope = scopeFactory.CreateScope();
            var codeCheckerAgent = scope.ServiceProvider.GetRequiredService<ICodeCheckerAgent>();
            var feedbackRepo = scope.ServiceProvider.GetRequiredService<IFeedbackRepository>();

            var agentInput = new CodeCheckerAgentInput(
                SubmissionId: submissionId,
                Code: code,
                Language: language,
                ProblemTitle: problemTitle,
                ProblemStatement: problemStatement);

            var feedbackItems = await codeCheckerAgent.AnalyzeAsync(agentInput, cancellationToken);

            var records = feedbackItems.Select(item =>
                FeedbackRecord.Create(submissionId, item.FeedbackType, item.Message, item.Confidence)).ToList();

            await feedbackRepo.AddRangeAsync(records);
            await feedbackRepo.SaveChangesAsync();

            logger.LogInformation(
                "✅ CodeChecker saved {Count} feedback records for submission {SubmissionId}.",
                feedbackItems.Count, submissionId);
        }
        catch (Exception ex)
        {
            // Never let a Code Checker failure affect an already-evaluated submission.
            logger.LogError(ex,
                "CodeChecker background task failed for submission {SubmissionId}.",
                submissionId);
        }
    }

    private static string NormalizeOutput(string output) =>
        output?.Trim().Replace("\r\n", "\n").Replace("\r", "\n").Replace(" ", "") ?? string.Empty;
}
