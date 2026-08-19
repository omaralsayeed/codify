using Codify.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.BackgroundJobs;

/// <summary>
/// Hosted worker that drains ISubmissionEvaluationQueue and runs each submission through
/// IJudgeEvaluationService. Registered as a hosted service, so it starts with the app and
/// keeps running for the app's lifetime (via ExecuteAsync's while loop over stoppingToken).
///
/// IJudgeEvaluationService (and everything it depends on — DbContext, repositories, etc.)
/// is Scoped, so we resolve it through a fresh IServiceScope per dequeued submission rather
/// than injecting it directly into this Singleton-lifetime worker.
///
/// A failure evaluating one submission is logged and swallowed so the worker keeps
/// processing the rest of the queue — one bad submission must never stop the pipeline.
/// </summary>
public class SubmissionEvaluationBackgroundService(
    ISubmissionEvaluationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<SubmissionEvaluationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [WORKER] Submission evaluation background worker STARTED and waiting for submissions...");

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid submissionId;
            try
            {
                logger.LogInformation("⏳ [WORKER] Waiting for next submission in queue...");
                submissionId = await queue.DequeueAsync(stoppingToken);
                logger.LogInformation("📥 [WORKER] Dequeued submission {SubmissionId} from queue", submissionId);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("🛑 [WORKER] Background worker cancelled (application shutting down)");
                break; // app shutting down
            }

            try
            {
                logger.LogInformation("🔧 [WORKER] Creating scope for submission {SubmissionId}...", submissionId);
                using var scope = scopeFactory.CreateScope();
                var evaluationService = scope.ServiceProvider.GetRequiredService<IJudgeEvaluationService>();
                
                logger.LogInformation("▶️  [WORKER] Starting evaluation for submission {SubmissionId}...", submissionId);
                await evaluationService.EvaluateSubmissionAsync(submissionId, stoppingToken);
                
                logger.LogInformation("✅ [WORKER] Submission {SubmissionId} evaluation completed successfully", submissionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "🔴 [WORKER] Judge0 evaluation FAILED for submission {SubmissionId}. ErrorType={ErrorType}, Message={Message}. " +
                    "The submission will remain in its last saved status.",
                    submissionId, ex.GetType().Name, ex.Message);
            }
        }

        logger.LogInformation("🛑 [WORKER] Submission evaluation background worker STOPPED.");
    }
}
