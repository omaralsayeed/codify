using Codify.Application.DTOs.Execution;

namespace Codify.Application.Interfaces;

/// <summary>
/// Defines the contract for the Day-2 skeleton quick-run execution service.
/// The implementation will be replaced with the real Docker-based runner in a future sprint.
/// </summary>
public interface IQuickRunService
{
    /// <summary>
    /// Accepts a code execution request and returns an acknowledgement.
    /// No actual execution occurs in this skeleton implementation.
    /// </summary>
    Task<QuickRunResponse> RunAsync(QuickRunRequest request);
}
