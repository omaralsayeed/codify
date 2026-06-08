using Codify.Application.DTOs.Execution;

namespace Codify.Application.Interfaces;

/// <summary>
/// Runs code against multiple test cases and returns pass/fail for each one.
/// </summary>
public interface IQuickRunWithTestsService
{
    Task<QuickRunWithTestsResponse> RunAsync(QuickRunWithTestsRequest request);
}
