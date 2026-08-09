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
        logger.LogInformation("Submission evaluation background worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid submissionId;
            try
            {
                submissionId = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break; // app shutting down
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var evaluationService = scope.ServiceProvider.GetRequiredService<IJudgeEvaluationService>();
                await evaluationService.EvaluateSubmissionAsync(submissionId, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Judge0 evaluation failed for submission {SubmissionId}. The submission will remain in its last saved status.",
                    submissionId);
            }
        }

        logger.LogInformation("Submission evaluation background worker stopped.");
    }
}
