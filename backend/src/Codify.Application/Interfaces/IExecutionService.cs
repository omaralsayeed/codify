using Codify.Application.DTOs.Execution;

namespace Codify.Application.Interfaces;

public interface IExecutionService
{
    /// <summary>Run code against sample test cases only (for the "Run" button).</summary>
    Task<RunCodeResponse> RunAsync(RunCodeRequest request);

    /// <summary>
    /// Evaluate code against a single test case input.
    /// Returns the actual stdout output and execution metadata.
    /// </summary>
    Task<TestCaseExecutionResult> EvaluateAsync(
        string code,
        string language,
        string input,
        int timeLimitMs,
        int memoryLimitMb,
        CancellationToken cancellationToken = default);
}
