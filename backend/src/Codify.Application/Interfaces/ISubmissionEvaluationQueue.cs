namespace Codify.Application.Interfaces;

/// <summary>
/// Queues submissions for asynchronous evaluation against Judge0.
/// SubmissionService enqueues a submission id right after persisting it as Pending;
/// a hosted background service (Infrastructure) dequeues and runs IJudgeEvaluationService.
/// </summary>
public interface ISubmissionEvaluationQueue
{
    void QueueSubmission(Guid submissionId);

    Task<Guid> DequeueAsync(CancellationToken cancellationToken);
}
