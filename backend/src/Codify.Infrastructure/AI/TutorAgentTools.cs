using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.AI;

/// <summary>
/// Implements the 4 tools the agentic Tutor Agent can call via OpenAI
/// function calling. The LLM decides which tools to call; these methods
/// only execute the requested tool and return structured data.
/// </summary>
public class TutorAgentTools(
    ISubmissionRepository submissionRepo,
    IHintLogRepository hintLogRepo,
    IKnowledgeBaseSearchService knowledgeBase,
    ILogger<TutorAgentTools> logger) : ITutorAgentTools
{
    private readonly ISubmissionRepository _submissionRepo = submissionRepo;
    private readonly IHintLogRepository _hintLogRepo = hintLogRepo;
    private readonly IKnowledgeBaseSearchService _knowledgeBase = knowledgeBase;
    private readonly ILogger<TutorAgentTools> _logger = logger;

    public async Task<AttemptHistoryResult> GetAttemptHistoryAsync(Guid studentId, Guid problemId)
    {
        var submissions = await _submissionRepo.GetByProblemAndUserAsync(problemId, studentId);
        var hintLogs = await _hintLogRepo.GetByUserAndProblemAsync(studentId, problemId);

        var subList = submissions.ToList();
        var hintList = hintLogs.ToList();

        return new AttemptHistoryResult
        {
            AttemptCount = subList.Count,
            SubmissionStatuses = subList.Select(s => s.Status.ToString()).ToList(),
            PreviousHintLevels = hintList.Select(h => h.HintLevel).ToList(),
            Timestamps = subList.Select(s => s.SubmittedAt.ToString("O")).ToList()
        };
    }

    public async Task<List<KnowledgeBaseResult>> SearchKnowledgeBaseAsync(string query, string? conceptTag)
    {
        return await _knowledgeBase.SearchAsync(query, conceptTag);
    }

    public Task<PartialCodeObservation> CheckPartialCodeAsync(string code, string language)
    {
        var observation = new PartialCodeObservation { SyntaxValid = true };
        var lang = (language ?? string.Empty).ToLowerInvariant();
        var codeLower = (code ?? string.Empty).ToLowerInvariant();
        var observations = new List<string>();

        var hasFor = codeLower.Contains("for ") || codeLower.Contains("for(");
        var hasWhile = codeLower.Contains("while ") || codeLower.Contains("while(");
        if (!hasFor && !hasWhile)
            observations.Add("No loop detected in the submitted code.");

        var hasRecursion = _detectRecursion(code ?? string.Empty, lang);
        if (hasRecursion)
        {
            observations.Add("Recursive call present.");
            var hasBaseCase = codeLower.Contains("if ") && (codeLower.Contains("return")
                || codeLower.Contains("== 0") || codeLower.Contains("== 1")
                || codeLower.Contains("<= 1") || codeLower.Contains("< 2"));
            if (!hasBaseCase)
                observations.Add("Recursive call present but no obvious base case detected.");
        }

        var hasHashMap = codeLower.Contains("dict") || codeLower.Contains("dictionary")
            || codeLower.Contains("hashmap") || codeLower.Contains("hash map");
        if (!hasHashMap)
            observations.Add("No hash map/dictionary usage detected.");

        if (lang.Contains("py") || lang.Contains("python"))
        {
            var colonCount = (code ?? string.Empty).Count(c => c == ':');
            if (colonCount == 0 && codeLower.Length > 10)
                observations.Add("No colons found - possible syntax issue in Python code.");
        }

        observation.Structure = string.Join("; ", observations);
        observation.Observations = observations;
        return Task.FromResult(observation);
    }

    public async Task<List<PreviousHintItem>> GetPreviousHintsAsync(Guid studentId, Guid problemId)
    {
        var hintLogs = await _hintLogRepo.GetByUserAndProblemAsync(studentId, problemId);
        return hintLogs.Select(h => new PreviousHintItem
        {
            HintLevel = h.HintLevel,
            HintText = h.ResponseText,
            GivenAt = h.CreatedAt.ToString("O")
        }).ToList();
    }

    private static bool _detectRecursion(string code, string lang)
    {
        var lowerLang = lang.ToLowerInvariant();
        if (lowerLang.Contains("py") || lowerLang.Contains("python"))
            return _detectPythonRecursion(code);

        // C# / Java-style methods: extract identifiers followed by '(' that
        // also appear as a method declaration and are called inside the body.
        return _detectCStyleRecursion(code);
    }

    private static bool _detectPythonRecursion(string code)
    {
        var lines = code.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
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

    private static bool _detectCStyleRecursion(string code)
    {
        var lines = code.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            // Match: "public int Fib(int n)" or "int Fib(int n)" or "static void Foo(...)"
            if (trimmed.Contains('(') && trimmed.Contains(')'))
            {
                // Remove access modifiers, return types, and "static" heuristically by
                // finding the token immediately before the first '('.
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

