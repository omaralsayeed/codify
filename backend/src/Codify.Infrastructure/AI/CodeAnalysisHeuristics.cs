namespace Codify.Infrastructure.AI;

/// <summary>
/// Deterministic static observations about a code submission. These features feed
/// the Code Analysis Agent so the LLM reasons over measured signals instead of
/// guessing — including signals commonly associated with AI-generated code.
/// This is intentionally lightweight (not a full parser) and language-agnostic.
/// </summary>
public class CodeAnalysisHeuristics
{
    public int TotalLines { get; set; }
    public int CodeLines { get; set; }
    public int CommentLines { get; set; }
    public int BlankLines { get; set; }
    public double CommentRatio { get; set; }
    public double AverageLineLength { get; set; }
    public int MaxIndentDepth { get; set; }

    /// <summary>Heuristic 0..1 score of AI-generation likelihood from static signals.</summary>
    public double AiLikelihoodHeuristic { get; set; }

    /// <summary>Human-readable signals the LLM can weigh.</summary>
    public List<string> Observations { get; set; } = [];

    public static CodeAnalysisHeuristics Analyze(string code, string language)
    {
        var result = new CodeAnalysisHeuristics();
        if (string.IsNullOrWhiteSpace(code))
            return result;

        var lang = (language ?? string.Empty).ToLowerInvariant();
        var lines = code.Replace("\r\n", "\n").Split('\n');

        result.TotalLines = lines.Length;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0) { result.BlankLines++; continue; }

            if (IsCommentLine(line, lang))
            {
                result.CommentLines++;
                continue;
            }

            result.CodeLines++;
            var indent = LeadingWhitespace(raw);
            result.MaxIndentDepth = Math.Max(result.MaxIndentDepth, indent / 4);
        }

        result.CommentRatio = result.TotalLines > 0
            ? Math.Round((double)result.CommentLines / result.TotalLines, 3)
            : 0;

        result.AverageLineLength = lines.Length > 0
            ? Math.Round(lines.Average(l => l.Length), 1)
            : 0;

        ComputeAiSignals(code, lang, result);
        return result;
    }

    // ── Private ───────────────────────────────────────────────────

    private static void ComputeAiSignals(string code, string lang, CodeAnalysisHeuristics result)
    {
        var lower = code.ToLowerInvariant();
        double score = 0.0;

        // Signal 1: high, uniform comment density is common in AI-generated solutions.
        if (result.CommentRatio >= 0.20)
        {
            result.Observations.Add($"High comment density ({result.CommentRatio:P0} of lines are comments).");
            score += 0.25;
        }

        // Signal 2: docstring / summary-style headers ("Approach:", "Explanation:").
        var summaryMarkers = new[] { "approach:", "explanation:", "algorithm:", "time complexity:", "space complexity:", "summary:" };
        if (summaryMarkers.Any(lower.Contains))
        {
            result.Observations.Add("Contains structured explanation markers (e.g. 'Approach:', 'Time complexity:').");
            score += 0.20;
        }

        // Signal 3: no debug artefacts — student code often has prints/TODOs/struck-through attempts.
        var hasDebugArtefacts = lower.Contains("console.log") || lower.Contains("print(") && lower.Contains("debug")
            || lower.Contains("todo") || lower.Contains("fixme") || lower.Contains("// cout") || lower.Contains("# debug");
        if (!hasDebugArtefacts && result.CodeLines > 8)
        {
            result.Observations.Add("No debug prints, TODOs, or iterative trial artefacts detected.");
            score += 0.15;
        }

        // Signal 4: very consistent indentation and clean structure.
        if (result.MaxIndentDepth >= 1 && result.BlankLines == 0 && result.CodeLines > 10)
        {
            result.Observations.Add("Uniform formatting with no blank-line separators.");
            score += 0.10;
        }

        // Signal 5: overly descriptive identifiers (camelCase full words) — mild signal.
        if (ContainsDescriptiveIdentifiers(code))
        {
            result.Observations.Add("Identifiers use fully descriptive multi-word names.");
            score += 0.10;
        }

        // Language-specific: Python docstrings are a mild AI signal.
        if (lang.Contains("py") && code.Contains("\"\"\""))
        {
            result.Observations.Add("Uses triple-quoted docstrings.");
            score += 0.10;
        }

        result.AiLikelihoodHeuristic = Math.Round(Math.Min(score, 1.0), 2);
    }

    private static bool ContainsDescriptiveIdentifiers(string code)
    {
        var descriptive = new[] { "result", "current", "previous", "index", "count", "maximum", "minimum", "length", "target", "answer" };
        var lower = code.ToLowerInvariant();
        return descriptive.Count(word => lower.Contains(word)) >= 3;
    }

    private static bool IsCommentLine(string trimmedLine, string lang)
    {
        if (trimmedLine.StartsWith("//") || trimmedLine.StartsWith("/*") || trimmedLine.StartsWith("*"))
            return true;
        if (trimmedLine.StartsWith('#') && (lang.Contains("py") || lang.Contains("csharp") == false))
            return true;
        if (lang.Contains("py") && trimmedLine.StartsWith("#"))
            return true;
        return false;
    }

    private static int LeadingWhitespace(string line)
    {
        int count = 0;
        foreach (var c in line)
        {
            if (c == ' ') count++;
            else if (c == '\t') count += 4;
            else break;
        }
        return count;
    }
}
