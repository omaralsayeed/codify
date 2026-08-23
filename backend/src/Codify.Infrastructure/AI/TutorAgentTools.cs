using Codify.Application.Agents;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.AI;

/// <summary>
/// Implements the Tutor Agent's tools. Each method executes exactly one tool and
/// returns structured data. The LLM decides which tools to call — this class
/// contains no hint-level branching logic.
/// </summary>
public class TutorAgentTools(
    ISubmissionRepository submissionRepo,
    IHintRepository hintRepo,
    IKnowledgeBaseSearchService knowledgeBase,
    ILogger<TutorAgentTools> logger) : ITutorAgentTools
{
    public async Task<AttemptHistoryResult> GetAttemptHistoryAsync(Guid studentId, Guid problemId)
    {
        logger.LogDebug("Fetching attempt history for user {UserId} on problem {ProblemId}", studentId, problemId);
        var submissions = (await submissionRepo.GetByProblemAndUserAsync(problemId, studentId)).ToList();
        var hintLogs = (await hintRepo.GetByUserAndProblemAsync(studentId, problemId)).ToList();

        return new AttemptHistoryResult
        {
            AttemptCount = submissions.Count,
            SubmissionStatuses = submissions.Select(s => s.Status.ToString()).ToList(),
            PreviousHintLevels = hintLogs.Select(h => h.HintLevel).ToList(),
            Timestamps = submissions.Select(s => s.SubmittedAt.ToString("O")).ToList()
        };
    }

    public async Task<List<KnowledgeBaseResult>> SearchKnowledgeBaseAsync(string query, string? conceptTag)
    {
        try
        {
            // Add 2-second timeout to prevent cold start delays from blocking hint generation
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            return await knowledgeBase.SearchAsync(query, conceptTag, cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("⏱️  Knowledge base search timed out after 2s for query '{Query}', falling back to empty results", query);
            return new List<KnowledgeBaseResult>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "⚠️  Knowledge base search failed for query '{Query}', falling back to empty results", query);
            return new List<KnowledgeBaseResult>();
        }
    }

    public Task<PartialCodeObservation> CheckPartialCodeAsync(string code, string language)
    {
        var observation = new PartialCodeObservation { SyntaxValid = true };
        var lang = (language ?? string.Empty).ToLowerInvariant();
        var codeLower = (code ?? string.Empty).ToLowerInvariant();
        var observations = new List<string>();

        // Loop detection
        var hasLoop = codeLower.Contains("for ") || codeLower.Contains("for(")
            || codeLower.Contains("while ") || codeLower.Contains("while(")
            || codeLower.Contains("foreach");
        if (!hasLoop)
            observations.Add("No loop detected in the submitted code.");

        // Recursion detection + base case
        if (DetectRecursion(code ?? string.Empty, lang))
        {
            observations.Add("Recursive call present.");
            var hasBaseCase = codeLower.Contains("if ") &&
                (codeLower.Contains("return") || codeLower.Contains("== 0")
                 || codeLower.Contains("== 1") || codeLower.Contains("<= 1") || codeLower.Contains("< 2"));
            if (!hasBaseCase)
                observations.Add("Recursive call present but no obvious base case detected.");
        }

        // Hash map / dictionary usage
        var hasHashMap = codeLower.Contains("dict") || codeLower.Contains("dictionary")
            || codeLower.Contains("hashmap") || codeLower.Contains("hash map")
            || codeLower.Contains("hashset") || codeLower.Contains("hash set");
        if (!hasHashMap)
            observations.Add("No hash map/dictionary/set usage detected.");

        // Very rough Python syntax heuristic
        if (lang.Contains("py") && codeLower.Length > 10 && !(code ?? string.Empty).Contains(':'))
            observations.Add("No colons found - possible syntax issue in Python code.");

        observation.Structure = string.Join("; ", observations);
        observation.Observations = observations;
        return Task.FromResult(observation);
    }

    public async Task<List<PreviousHintItem>> GetPreviousHintsAsync(Guid studentId, Guid problemId)
    {
        var hintLogs = (await hintRepo.GetByUserAndProblemAsync(studentId, problemId)).ToList();
        return hintLogs.Select(h => new PreviousHintItem
        {
            HintLevel = h.HintLevel,
            HintText = h.ResponseText,
            GivenAt = h.CreatedAt.ToString("O")
        }).ToList();
    }

    private static bool DetectRecursion(string code, string lang)
    {
        if (lang.Contains("py"))
            return DetectPythonRecursion(code);
        return DetectCStyleRecursion(code);
    }

    private static bool DetectPythonRecursion(string code)
    {
        foreach (var line in code.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("def ") && trimmed.Contains('('))
            {
                var name = trimmed[4..].Split('(')[0].Trim();
                if (name.Length > 0 && code.Contains(name + "("))
                    return true;
            }
        }
        return false;
    }

    private static bool DetectCStyleRecursion(string code)
    {
        foreach (var line in code.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Contains('(') && trimmed.Contains(')'))
            {
                var beforeParen = trimmed.Split('(')[0].Trim();
                var parts = beforeParen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var name = parts[^1];
                    if (name.Length > 0 && code.Contains(name + "("))
                        return true;
                }
            }
        }
        return false;
    }
}
