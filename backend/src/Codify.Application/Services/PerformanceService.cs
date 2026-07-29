using System.Text.Json;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;

namespace Codify.Application.Services;

public class PerformanceService(
    IPerformanceRepository performanceRepo,
    ISubmissionRepository submissionRepo) : IPerformanceService
{
    private const float WeakThreshold = 0.40f;
    private const float StrongThreshold = 0.75f;

    public async Task UpdateAfterSubmissionAsync(Guid userId)
    {
        // Load all non-deleted submissions for this user with their problem tags
        var submissions = (await submissionRepo.GetAllByUserAsync(userId)).ToList();

        if (submissions.Count == 0) return;

        var total = submissions.Count;
        var accepted = submissions.Count(s => s.Status == SubmissionStatus.Accepted);

        float successRate = (float)accepted / total;

        // Average attempts = total submissions / distinct problems attempted
        var distinctProblems = submissions.Select(s => s.ProblemId).Distinct().Count();
        float averageAttempts = distinctProblems > 0 ? (float)total / distinctProblems : 0f;

        // Per-tag performance: group submissions by concept tag
        // Each submission contributes to all tags of its problem
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

        var weakJson = JsonSerializer.Serialize(weakTopics);
        var strongJson = JsonSerializer.Serialize(strongTopics);

        // Upsert the profile
        var profile = await performanceRepo.GetByUserIdAsync(userId);
        if (profile is null)
        {
            profile = PerformanceProfile.CreateForUser(userId);
            profile.Update(weakJson, strongJson, successRate, averageAttempts);
            await performanceRepo.AddAsync(profile);
        }
        else
        {
            profile.Update(weakJson, strongJson, successRate, averageAttempts);
        }

        await performanceRepo.SaveChangesAsync();
    }
}
