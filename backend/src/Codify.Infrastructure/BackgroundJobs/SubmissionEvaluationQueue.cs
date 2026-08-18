using System.Threading.Channels;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.BackgroundJobs;

/// <summary>
/// In-process FIFO queue of submission ids awaiting Judge0 evaluation, backed by
/// System.Threading.Channels. Registered as a Singleton; SubmissionEvaluationBackgroundService
/// is the sole consumer.
/// Unbounded by design — SubmissionsController already rate-limits POST /submissions
/// (30/hour/user), so the queue can never grow unboundedly fast in practice.
/// </summary>
public class SubmissionEvaluationQueue(ILogger<SubmissionEvaluationQueue> logger) : ISubmissionEvaluationQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public void QueueSubmission(Guid submissionId)
    {
        logger.LogInformation("📬 [QUEUE] Queuing submission {SubmissionId} for background evaluation", submissionId);
        
        if (!_channel.Writer.TryWrite(submissionId))
        {
            logger.LogError("🔴 [QUEUE] FAILED to queue submission {SubmissionId}!", submissionId);
            throw new InvalidOperationException("Failed to queue submission for evaluation.");
        }
        
        logger.LogInformation("✅ [QUEUE] Submission {SubmissionId} queued successfully", submissionId);
    }

    public async Task<Guid> DequeueAsync(CancellationToken cancellationToken) =>
        await _channel.Reader.ReadAsync(cancellationToken);
}
