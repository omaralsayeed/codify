using System.Globalization;
using Codify.Application.DTOs.Execution;
using Codify.Application.Execution;
using Codify.Application.Interfaces;
using Codify.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Codify.Application.Services;

/// <summary>
/// RunAsync      → used by POST /execution/run ("Run" button, sample cases only).
/// EvaluateAsync → used by the submission pipeline (via IJudgeEvaluationService) to judge
///                 one test case at a time. Backed by Judge0 — see IJudge0Client / Judge0Client.
/// </summary>
public class ExecutionService(
    IProblemRepository problemRepo,
    IJudge0Client judge0Client,
    ILogger<ExecutionService> logger) : IExecutionService
{
    public async Task<RunCodeResponse> RunAsync(RunCodeRequest request)
    {
        var problem = await problemRepo.GetByIdWithTestCasesAsync(request.ProblemId)
            ?? throw new NotFoundException($"Problem {request.ProblemId} not found.");

        var sampleCases = problem.TestCases
            .Where(tc => tc.IsSample)
            .OrderBy(tc => tc.OrderIndex)
            .ToList();

        if (sampleCases.Count == 0)
        {
            return new RunCodeResponse
            {
                Status = "NoSampleCases",
                Stdout = string.Empty,
                Stderr = "No sample test cases available for this problem.",
                ExecutionTimeMs = 0,
                TestResults = []
            };
        }

        var results = new List<SampleTestResult>();
        var totalExecTimeMs = 0;
        string? firstStderr = null;

        foreach (var tc in sampleCases)
        {
            var eval = await EvaluateAsync(
                request.Code,
                request.Language.ToString(),
                tc.InputData,
                problem.TimeLimitMs,
                problem.MemoryLimitMb);

            totalExecTimeMs += eval.ExecutionTimeMs;
            if (!string.IsNullOrWhiteSpace(eval.Stderr))
                firstStderr ??= eval.Stderr;

            results.Add(new SampleTestResult
            {
                Input = tc.InputData,
                ExpectedOutput = tc.ExpectedOutput,
                ActualOutput = eval.ActualOutput,
                Passed = !eval.TimedOut && !eval.CompileError && !eval.RuntimeError
                         && NormalizeOutput(eval.ActualOutput) == NormalizeOutput(tc.ExpectedOutput)
            });
        }

        var allPassed = results.All(r => r.Passed);
        return new RunCodeResponse
        {
            Status = allPassed ? "Accepted" : "WrongAnswer",
            Stdout = results.FirstOrDefault()?.ActualOutput ?? string.Empty,
            Stderr = firstStderr ?? string.Empty,
            ExecutionTimeMs = totalExecTimeMs,
            TestResults = results
        };
    }

    public async Task<TestCaseExecutionResult> EvaluateAsync(
        string code,
        string language,
        string input,
        int timeLimitMs,
        int memoryLimitMb,
        CancellationToken cancellationToken = default)
    {
        var languageId = Judge0LanguageMap.GetLanguageId(language);
        if (languageId is null)
        {
            logger.LogWarning("Judge0 evaluation requested for unsupported language '{Language}'.", language);
            return new TestCaseExecutionResult
            {
                ActualOutput = string.Empty,
                Stderr = $"Language '{language}' is not supported. Supported: {Judge0LanguageMap.SupportedLanguages}.",
                RuntimeError = true
            };
        }

        var request = new Judge0SubmissionRequest
        {
            SourceCode = code,
            LanguageId = languageId.Value,
            Stdin = input,
            CpuTimeLimitSeconds = Math.Max(1, timeLimitMs) / 1000d,
            MemoryLimitKb = Math.Max(1, memoryLimitMb) * 1024
        };

        Judge0SubmissionResult result;
        try
        {
            result = await judge0Client.ExecuteAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Judge0 execution failed for language {Language}.", language);
            return new TestCaseExecutionResult
            {
                ActualOutput = string.Empty,
                Stderr = "Judge0 execution failed: " + ex.Message,
                RuntimeError = true
            };
        }

        return MapToExecutionResult(result);
    }

    // ── Private ───────────────────────────────────────────────────

    private static TestCaseExecutionResult MapToExecutionResult(Judge0SubmissionResult result)
    {
        var executionTimeMs = ParseTimeToMs(result.TimeSeconds);
        var memoryUsedKb = result.MemoryKb ?? 0;

        if (result.PollTimedOut || result.StatusId == Judge0Status.TimeLimitExceeded)
        {
            return new TestCaseExecutionResult
            {
                ActualOutput = result.Stdout ?? string.Empty,
                Stderr = result.Stderr ?? string.Empty,
                ExecutionTimeMs = executionTimeMs,
                MemoryUsedKb = memoryUsedKb,
                TimedOut = true
            };
        }

        if (result.StatusId == Judge0Status.CompilationError)
        {
            return new TestCaseExecutionResult
            {
                ActualOutput = string.Empty,
                Stderr = result.CompileOutput ?? result.Message ?? "Compilation failed.",
                ExecutionTimeMs = executionTimeMs,
                CompileError = true
            };
        }

        if (Judge0Status.IsRuntimeError(result.StatusId)
            || result.StatusId == Judge0Status.InternalError
            || result.StatusId == Judge0Status.ExecFormatError)
        {
            return new TestCaseExecutionResult
            {
                ActualOutput = result.Stdout ?? string.Empty,
                Stderr = result.Stderr ?? result.Message ?? "Runtime error.",
                ExecutionTimeMs = executionTimeMs,
                MemoryUsedKb = memoryUsedKb,
                RuntimeError = true
            };
        }

        // Accepted (3) or WrongAnswer (4) — Codify does its own output comparison
        // (see JudgeEvaluationService), so both map to a plain result here.
        return new TestCaseExecutionResult
        {
            ActualOutput = result.Stdout ?? string.Empty,
            Stderr = result.Stderr ?? string.Empty,
            ExecutionTimeMs = executionTimeMs,
            MemoryUsedKb = memoryUsedKb
        };
    }

    private static int ParseTimeToMs(string? timeSeconds) =>
        double.TryParse(timeSeconds, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            ? (int)Math.Round(seconds * 1000)
            : 0;

    private static string NormalizeOutput(string output) =>
        output.Trim().Replace("\r\n", "\n").Replace("\r", "\n");
}
