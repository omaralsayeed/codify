namespace Codify.Application.Interfaces;

/// <summary>
/// Runs one submission through the full evaluation pipeline:
/// fetches public + hidden test cases, executes each against Judge0, stores a
/// per-test-case verdict, rolls the results up into the submission's final status,
/// updates problem/user/performance counters, and triggers the Code Checker Agent.
/// Invoked by the background queue worker — see ISubmissionEvaluationQueue.
/// </summary>
public interface IJudgeEvaluationService
{
    Task EvaluateSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default);
}
