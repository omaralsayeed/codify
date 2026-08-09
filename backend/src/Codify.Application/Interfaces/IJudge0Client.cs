using Codify.Application.DTOs.Execution;

namespace Codify.Application.Interfaces;

/// <summary>
/// Contract for talking to a Judge0 instance (self-hosted or RapidAPI-hosted).
/// Lives in Application layer — Infrastructure provides the real HTTP implementation.
/// </summary>
public interface IJudge0Client
{
    /// <summary>
    /// Submits source code + stdin to Judge0, then polls the submission until Judge0
    /// reports a terminal status (Accepted / Wrong Answer / TLE / error / etc.) or the
    /// configured polling budget is exhausted.
    /// </summary>
    Task<Judge0SubmissionResult> ExecuteAsync(
        Judge0SubmissionRequest request,
        CancellationToken cancellationToken = default);
}
