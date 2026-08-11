using System.Text.Json;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;

namespace Codify.Application.Services;

public class PerformanceService(
    IPerformanceRepository performanceRepo,
    ISubmissionRepository submissionRepo,
    IHintRepository hintRepo) : IPerformanceService
{
    private const float WeakThreshold   = 0.40f;
    private const float StrongThreshold = 0.75f;

    // ─────────────────────────────────────────────────────────────────────────
    // Full recalculation — called after every submission evaluation
    // ─────────────────────────────────────────────────────────────────────────

    public async Task UpdateAfterSubmissionAsync(Guid userId)
    {
        var submissions = (await submissionRepo.GetAllByUserAsync(userId)).ToList();
        if (submissions.Count == 0) return;

        var total    = submissions.Count;
        var accepted = submissions.Count(s => s.Status == SubmissionStatus.Accepted);

        float successRate = (float)accepted / total;

        // Average attempts = total submissions / distinct problems attempted
        int distinctProblems  = submissions.Select(s => s.ProblemId).Distinct().Count();
        float averageAttempts = distinctProblems > 0 ? (float)total / distinctProblems : 0f;

        // Per-tag performance: each submission counts toward every concept tag on its problem
        var tagStats = new Dictionary<string, (int Attempts, int Accepted)>();

        foreach (var submission in submissions)
        {
            foreach (var tag in submission.Problem.ProblemTags.Select(pt => pt.ConceptTag.Name))
            {
                if (!tagStats.ContainsKey(tag))
                    tagStats[tag] = (0, 0);

                var (attempts, acceptedCount) = tagStats[tag];
                tagStats[tag] = (
                    attempts + 1,
                    acceptedCount + (submission.Status == SubmissionStatus.Accepted ? 1 : 0)
                );
            }
        }

        var weakTopics = tagStats
            .Where(kv => kv.Value.Attempts > 0 &&
                         (float)kv.Value.Accepted / kv.Value.Attempts < WeakThreshold)
            .Select(kv => kv.Key)
            .OrderBy(t => t)
            .ToList();

        var strongTopics = tagStats
            .Where(kv => kv.Value.Attempts > 0 &&
                         (float)kv.Value.Accepted / kv.Value.Attempts > StrongThreshold)
            .Select(kv => kv.Key)
            .OrderBy(t => t)
            .ToList();

        // Include current hint count so the profile stays in sync
        int totalHints = await hintRepo.CountByUserAsync(userId);

        var weakJson   = JsonSerializer.Serialize(weakTopics);
        var strongJson = JsonSerializer.Serialize(strongTopics);

        // Upsert the profile
        var profile = await performanceRepo.GetByUserIdAsync(userId);
        if (profile is null)
        {
            profile = PerformanceProfile.CreateForUser(userId);
            profile.Update(weakJson, strongJson, successRate, averageAttempts, totalHints);
            await performanceRepo.AddAsync(profile);
        }
        else
        {
            profile.Update(weakJson, strongJson, successRate, averageAttempts, totalHints);
        }

        await performanceRepo.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Lightweight hint-count increment — called after every hint is persisted
    // ─────────────────────────────────────────────────────────────────────────

    public async Task IncrementHintCountAsync(Guid userId)
    {
        var profile = await performanceRepo.GetByUserIdAsync(userId);
        if (profile is null)
        {
            // Profile doesn't exist yet — create a blank one with hint count = 1
            profile = PerformanceProfile.CreateForUser(userId);
            profile.IncrementHintCount();
            await performanceRepo.AddAsync(profile);
        }
        else
        {
            profile.IncrementHintCount();
        }

        await performanceRepo.SaveChangesAsync();
    }
}
