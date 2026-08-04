using Codify.Application.Agents;
using Codify.Application.Interfaces;
using Codify.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.AI;

public class AnalyticsAgentTools(
    ISubmissionRepository submissionRepo,
    IVectorStore vectorStore,
    IEmbeddingService embeddingService,
    ILogger<AnalyticsAgentTools> logger) : IAnalyticsAgentTools
{
    public async Task<List<SubmissionSnapshot>> GetSubmissionHistoryAsync(Guid userId)
    {
        logger.LogDebug("Fetching submission history for user {UserId}", userId);
        var submissions = await submissionRepo.GetAllByUserWithDetailsAsync(userId);
        return submissions.Select(MapToSnapshot).ToList();
    }

    public async Task<TopicPerformanceResult> AggregateTopicPerformanceAsync(Guid userId)
    {
        var submissions = await submissionRepo.GetAllByUserWithDetailsAsync(userId);
        var snapshots = submissions.Select(MapToSnapshot).ToList();

        var topicGroups = snapshots
            .SelectMany(s => s.Topics.Select(t => new { Topic = t, Snapshot = s }))
            .GroupBy(x => x.Topic)
            .ToList();

        var stats = new List<TopicStat>();
        foreach (var group in topicGroups)
        {
            var items = group.ToList();
            var solved = items.Count(x => x.Snapshot.Status == SubmissionStatus.Accepted.ToString());
            var failed = items.Count - solved;

            var attemptsByProblem = items
                .GroupBy(x => x.Snapshot.ProblemId)
                .Select(g => g.Count())
                .ToList();
            var avgAttempts = attemptsByProblem.Count > 0 ? (float)attemptsByProblem.Average() : 0f;

            var difficulties = items
                .Select(x => x.Snapshot.Difficulty)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .ToList();

            stats.Add(new TopicStat
            {
                Name = group.Key,
                Solved = solved,
                Failed = failed,
                SuccessRate = items.Count > 0 ? (float)solved / items.Count : 0f,
                AverageAttempts = avgAttempts,
                AverageDifficulty = difficulties.Count > 0 ? string.Join(", ", difficulties) : "Unknown"
            });
        }

        return new TopicPerformanceResult { Topics = stats.OrderByDescending(s => s.SuccessRate).ToList() };
    }

    public async Task<WeaknessDetectionResult> DetectWeaknessesAsync(Guid userId)
    {
        var submissions = await submissionRepo.GetAllByUserWithDetailsAsync(userId);
        var snapshots = submissions.Select(MapToSnapshot).ToList();
        var feedback = submissions
            .SelectMany(s => s.FeedbackRecords)
            .Select(f => new FeedbackSnapshot
            {
                FeedbackType = f.FeedbackType.ToString(),
                Message = f.Message,
                CreatedAt = f.CreatedAt
            }).ToList();

        var mistakes = new List<string>();
        var weakTopics = new List<string>();

        var statusCounts = snapshots
            .GroupBy(s => s.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        if (statusCounts.GetValueOrDefault(SubmissionStatus.CompileError.ToString()) > 2)
            mistakes.Add("Recurring compilation errors - review language syntax before solving.");

        if (statusCounts.GetValueOrDefault(SubmissionStatus.RuntimeError.ToString()) > 2)
            mistakes.Add("Recurring runtime errors - check edge cases and null/empty inputs.");

        if (statusCounts.GetValueOrDefault(SubmissionStatus.TimeLimitExceeded.ToString()) > 2)
            mistakes.Add("Frequent time limit exceeded - consider more efficient algorithms.");

        var topicStats = await AggregateTopicPerformanceAsync(userId);
        weakTopics = topicStats.Topics
            .Where(t => t.SuccessRate < 0.5f && t.Solved + t.Failed >= 2)
            .Select(t => t.Name)
            .ToList();

        var weaknessKeywords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["off-by-one"] = "Off-by-one errors - double-check loop boundaries and indices.",
            ["recursion"] = "Recursion mistakes - ensure base cases and termination.",
            ["dp"] = "Dynamic Programming transitions - practice state definition.",
            ["graph"] = "Graph traversal issues - verify visited tracking and adjacency building.",
            ["memory"] = "Memory usage - watch for unnecessary data structures."
        };

        foreach (var keyword in weaknessKeywords)
        {
            if (feedback.Any(f => f.Message.Contains(keyword.Key, StringComparison.OrdinalIgnoreCase)))
                mistakes.Add(keyword.Value);
        }

        return new WeaknessDetectionResult
        {
            CommonMistakes = mistakes.Distinct().ToList(),
            WeakTopics = weakTopics
        };
    }

    public async Task<TrendAnalysisResult> AnalyzeTrendsAsync(Guid userId)
    {
        var submissions = await submissionRepo.GetAllByUserWithDetailsAsync(userId);
        var snapshots = submissions.Select(MapToSnapshot).OrderBy(s => s.SubmittedAt).ToList();

        if (snapshots.Count < 4)
        {
            return new TrendAnalysisResult
            {
                Consistency = "Insufficient data",
                ImprovingTopics = [],
                DecliningTopics = []
            };
        }

        var midPoint = snapshots.Count / 2;
        var early = snapshots.Take(midPoint).ToList();
        var recent = snapshots.Skip(midPoint).ToList();

        var earlyTopicSuccess = ComputeTopicSuccessRates(early);
        var recentTopicSuccess = ComputeTopicSuccessRates(recent);

        var improving = new List<string>();
        var declining = new List<string>();

        foreach (var topic in recentTopicSuccess.Keys)
        {
            var earlyRate = earlyTopicSuccess.GetValueOrDefault(topic);
            var recentRate = recentTopicSuccess[topic];
            if (recentRate - earlyRate > 0.2f)
                improving.Add(topic);
            else if (earlyRate - recentRate > 0.2f)
                declining.Add(topic);
        }

        var activeDays = snapshots.Select(s => s.SubmittedAt.Date).Distinct().Count();
        var totalDays = (snapshots.Last().SubmittedAt - snapshots.First().SubmittedAt).Days + 1;
        var inactiveDays = Math.Max(0, totalDays - activeDays);
        var consistency = activeDays >= Math.Max(1, totalDays / 2) ? "High" : "Moderate";
        if (snapshots.Count < 5) consistency = "Low";

        var velocity = activeDays > 0 ? (float)snapshots.Count / activeDays : 0f;

        return new TrendAnalysisResult
        {
            ImprovingTopics = improving,
            DecliningTopics = declining,
            Consistency = consistency,
            LearningVelocity = velocity,
            ActiveDays = activeDays,
            InactiveDays = inactiveDays
        };
    }

    public async Task<TagGenerationResult> GenerateTagsAsync(Guid userId)
    {
        var topicStats = await AggregateTopicPerformanceAsync(userId);

        var strongTopics = topicStats.Topics
            .Where(t => t.SuccessRate >= 0.7f && t.Solved + t.Failed >= 2)
            .Select(t => t.Name)
            .ToList();

        var weakTopics = topicStats.Topics
            .Where(t => t.SuccessRate < 0.5f && t.Solved + t.Failed >= 2)
            .Select(t => t.Name)
            .ToList();

        var totalAttempts = topicStats.Topics.Sum(t => t.Solved + t.Failed);
        var overallSuccessRate = totalAttempts > 0
            ? (float)topicStats.Topics.Sum(t => t.Solved) / totalAttempts
            : 0f;

        var learningStage = overallSuccessRate switch
        {
            >= 0.8f => "Advanced",
            >= 0.5f => "Intermediate",
            _ => "Beginner"
        };

        var overallScore = Math.Clamp((int)(overallSuccessRate * 100), 0, 100);
        var confidence = Math.Clamp(totalAttempts / 20f, 0.1f, 1f);

        var recommendedDifficulty = overallSuccessRate switch
        {
            >= 0.8f => "Hard",
            >= 0.5f => "Medium",
            _ => "Easy"
        };

        return new TagGenerationResult
        {
            LearningStage = learningStage,
            OverallScore = overallScore,
            Consistency = "Unknown",
            Confidence = confidence,
            WeakTopics = weakTopics,
            StrongTopics = strongTopics,
            RecommendedDifficulty = recommendedDifficulty
        };
    }

    public async Task<ConceptContextResult> GetConceptContextAsync(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
            return new ConceptContextResult { Topic = topic, Summary = "No topic provided." };

        try
        {
            await vectorStore.EnsureCollectionAsync();
            var vector = await embeddingService.GenerateAsync(topic);
            var results = await vectorStore.SearchAsync(
                vector,
                conceptTag: topic,
                source: "concept",
                topK: 3,
                minSimilarity: 0.6f);

            var chunks = results.Select(r => r.Content).ToList();
            return new ConceptContextResult
            {
                Topic = topic,
                RetrievedChunks = chunks,
                Summary = chunks.Count > 0
                    ? $"Retrieved {chunks.Count} concept chunk(s) for '{topic}'."
                    : $"No relevant concept chunks found for '{topic}'."
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve concept context for topic '{Topic}'", topic);
            return new ConceptContextResult
            {
                Topic = topic,
                Summary = "Concept context retrieval failed."
            };
        }
    }

    public async Task<ProblemClassificationResult> ClassifyProblemTagsAsync(string problemTitle, string problemStatement)
    {
        if (string.IsNullOrWhiteSpace(problemStatement))
            return new ProblemClassificationResult
            {
                SuggestedTags = [],
                Reasoning = "No problem statement provided."
            };

        try
        {
            await vectorStore.EnsureCollectionAsync();
            var text = $"{problemTitle}\n\n{problemStatement}";
            var vector = await embeddingService.GenerateAsync(text);
            var results = await vectorStore.SearchAsync(
                vector,
                source: "problem",
                topK: 5,
                minSimilarity: 0.6f);

            var tagVotes = results
                .Where(r => r.Metadata.TryGetValue("concept_tag", out _))
                .GroupBy(r => r.Metadata["concept_tag"]?.ToString() ?? "General")
                .Select(g => new TagConfidence
                {
                    Tag = g.Key,
                    Confidence = (float)g.Average(r => r.Similarity)
                })
                .Where(tc => !string.IsNullOrWhiteSpace(tc.Tag))
                .OrderByDescending(tc => tc.Confidence)
                .Take(3)
                .ToList();

            return new ProblemClassificationResult
            {
                SuggestedTags = tagVotes.Select(t => t.Tag).ToList(),
                TagConfidences = tagVotes,
                Reasoning = $"Inferred from {results.Count} nearest tagged problem(s) in the vector store."
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to classify problem tags for '{Title}'", problemTitle);
            return new ProblemClassificationResult
            {
                SuggestedTags = [],
                Reasoning = "Problem classification failed."
            };
        }
    }

    private static Dictionary<string, float> ComputeTopicSuccessRates(List<SubmissionSnapshot> snapshots)
    {
        return snapshots
            .SelectMany(s => s.Topics.Select(t => new { Topic = t, Status = s.Status }))
            .GroupBy(x => x.Topic)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var total = g.Count();
                    var accepted = g.Count(x => x.Status == SubmissionStatus.Accepted.ToString());
                    return total > 0 ? (float)accepted / total : 0f;
                });
    }

    private static SubmissionSnapshot MapToSnapshot(Domain.Entities.Submission s) => new()
    {
        SubmissionId = s.Id,
        ProblemId = s.ProblemId,
        ProblemTitle = s.Problem?.Title ?? string.Empty,
        Difficulty = s.Problem?.Difficulty.ToString() ?? string.Empty,
        Topics = s.Problem?.ProblemTags.Select(pt => pt.ConceptTag.Name).ToList() ?? [],
        Language = s.Language.ToString(),
        Status = s.Status.ToString(),
        SubmittedAt = s.SubmittedAt,
        ExecutionTimeMs = s.ExecutionTimeMs,
        MemoryUsedKb = s.MemoryUsedKb,
        PassedTestCases = s.PassedTestCases,
        TotalTestCases = s.TotalTestCases,
        Score = s.Score
    };
}
