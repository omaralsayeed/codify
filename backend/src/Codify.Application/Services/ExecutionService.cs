using Codify.Application.DTOs.Execution;
using Codify.Application.Interfaces;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

/// <summary>
/// Stub execution service.
/// RunAsync  → used by POST /execution/run ("Run" button, sample cases only).
/// EvaluateAsync → used by the submission pipeline to judge one test case at a time.
/// TODO Sprint 2 (Badry): replace EvaluateAsync body with real Docker runner.
/// </summary>
public class ExecutionService(IProblemRepository problemRepo) : IExecutionService
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

        // Stub: evaluate each sample case
        var results = new List<SampleTestResult>();
        foreach (var tc in sampleCases)
        {
            var eval = await EvaluateAsync(
                request.Code,
                request.Language.ToString(),
                tc.InputData,
                problem.TimeLimitMs,
                problem.MemoryLimitMb);

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
            Stderr = string.Empty,
            ExecutionTimeMs = 0,
            TestResults = results
        };
    }

    public Task<TestCaseExecutionResult> EvaluateAsync(
        string code,
        string language,
        string input,
        int timeLimitMs,
        int memoryLimitMb,
        CancellationToken cancellationToken = default)
    {
        // ── STUB ──────────────────────────────────────────────────────────────
        // Badry replaces this with a real Docker-based runner.
        // The stub always returns an empty output so the submission pipeline
        // can exercise the full state machine without a real executor.
        // ─────────────────────────────────────────────────────────────────────
        return Task.FromResult(new TestCaseExecutionResult
        {
            ActualOutput = string.Empty,
            Stderr = string.Empty,
            ExecutionTimeMs = 0,
            MemoryUsedKb = 0,
            TimedOut = false,
            CompileError = false,
            RuntimeError = false
        });
    }

    private static string NormalizeOutput(string output) =>
        output.Trim().Replace("\r\n", "\n").Replace("\r", "\n");
}
