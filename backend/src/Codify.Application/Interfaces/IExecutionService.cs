using Codify.Application.DTOs.Execution;
using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IExecutionService
{
    /// <summary>Run code against sample test cases only (for the "Run" button).</summary>
    Task<RunCodeResponse> RunAsync(RunCodeRequest request);

    /// <summary>
    /// Evaluate code against a single test case input.
    /// Returns the actual stdout output and execution metadata.
    /// If problem is provided and has code templates, wraps user code automatically.
    /// </summary>
    Task<TestCaseExecutionResult> EvaluateAsync(
        string code,
        string language,
        string input,
        int timeLimitMs,
        int memoryLimitMb,
        Problem? problem = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluate code against multiple test cases in a single batch request to Judge0.
    /// This is significantly faster than sequential EvaluateAsync calls.
    /// Returns results in the same order as test cases.
    /// </summary>
    Task<IReadOnlyList<TestCaseExecutionResult>> EvaluateBatchAsync(
        string code,
        string language,
        IEnumerable<TestCase> testCases,
        int timeLimitMs,
        int memoryLimitMb,
        Problem? problem = null,
        CancellationToken cancellationToken = default);
}
