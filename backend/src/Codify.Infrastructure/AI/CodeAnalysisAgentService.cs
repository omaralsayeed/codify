using System.Text.Json;
using System.Text.Json.Serialization;
using Codify.Application.Agents;
using Codify.Application.Interfaces;
using Codify.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.AI;

/// <summary>
/// The Code Analysis Agent implemented as a STATIC workflow (not agentic tool
/// calling). It is fired after submission evaluation and runs a fixed pipeline:
///
///   deterministic heuristics -> single LLM call -> structured output ->
///   feedback items (+ optional AI-generated flag)
///
/// The deterministic heuristics ground the LLM so it reasons over measured
/// signals. The agent also assesses whether the code appears AI-generated and,
/// when confident, emits an <see cref="FeedbackType.AiGenerated"/> feedback item.
/// </summary>
public class CodeAnalysisAgentService(
    ILLMClient llmClient,
    IPromptLoader promptLoader,
    ILogger<CodeAnalysisAgentService> logger) : ICodeCheckerAgent
{
    private const string PromptFileName = "code-analysis-agent-system.txt";
    private const double AiGeneratedConfidenceThreshold = 0.6;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<CodeCheckerFeedbackItem>> AnalyzeAsync(
        CodeCheckerAgentInput input, CancellationToken cancellationToken = default)
    {
        // 1. Deterministic static-analysis signals (grounding for the LLM).
        var heuristics = CodeAnalysisHeuristics.Analyze(input.Code, input.Language);

        // 2. Render the prompt with the problem, code, and measured signals.
        var template = await promptLoader.LoadAsync(PromptFileName, cancellationToken);
        var systemPrompt = PromptTemplate.Render(template, new Dictionary<string, string>
        {
            ["problemTitle"] = input.ProblemTitle,
            ["problemStatement"] = input.ProblemStatement,
            ["language"] = input.Language,
            ["code"] = input.Code,
            ["heuristics"] = FormatHeuristics(heuristics)
        });

        // 3. Single LLM round-trip.
        string rawResponse;
        try
        {
            rawResponse = await llmClient.CompleteAsync(systemPrompt, "Return only the JSON response.", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Code Analysis Agent LLM call failed for submission {SubmissionId}.", input.SubmissionId);
            return Fallback();
        }

        // 4. Parse and validate the structured output.
        return ParseOutput(rawResponse, heuristics, input.SubmissionId);
    }

    // ── Private ───────────────────────────────────────────────────

    private IReadOnlyList<CodeCheckerFeedbackItem> ParseOutput(
        string rawResponse, CodeAnalysisHeuristics heuristics, Guid submissionId)
    {
        try
        {
            var cleaned = rawResponse
                .Replace("```json", string.Empty)
                .Replace("```", string.Empty)
                .Trim();

            var output = JsonSerializer.Deserialize<CodeAnalysisLlmOutput>(cleaned, JsonOptions);
            if (output is null)
            {
                logger.LogWarning("Code Analysis Agent deserialization returned null for submission {SubmissionId}.", submissionId);
                return Fallback();
            }

            var items = new List<CodeCheckerFeedbackItem>();

            foreach (var item in output.FeedbackItems ?? [])
            {
                if (!Enum.TryParse<FeedbackType>(item.FeedbackType, ignoreCase: true, out var feedbackType))
                {
                    logger.LogWarning("Code Analysis Agent returned unknown feedback type '{Type}'.", item.FeedbackType);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(item.Message))
                    items.Add(new CodeCheckerFeedbackItem(feedbackType, item.Message.Trim()));
            }

            // AI-generated detection: only flag when the model is confident.
            if (output.AiGenerated && output.AiGeneratedConfidence >= AiGeneratedConfidenceThreshold)
            {
                var note = string.IsNullOrWhiteSpace(output.AiGeneratedIndicators)
                    ? "Submission shows patterns commonly associated with AI-generated code."
                    : output.AiGeneratedIndicators.Trim();
                items.Add(new CodeCheckerFeedbackItem(
                    FeedbackType.AiGenerated,
                    $"Possible AI-generated code (confidence {output.AiGeneratedConfidence:P0}): {note}",
                    output.AiGeneratedConfidence));
            }

            // If the model returned nothing usable, fall back to the heuristic signal.
            if (items.Count == 0)
            {
                if (heuristics.AiLikelihoodHeuristic >= AiGeneratedConfidenceThreshold)
                {
                    items.Add(new CodeCheckerFeedbackItem(
                        FeedbackType.AiGenerated,
                        "Static signals suggest this submission may be AI-generated."));
                }
                else
                {
                    return Fallback();
                }
            }

            return items;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Code Analysis Agent JSON parse failed for submission {SubmissionId}.", submissionId);
            return Fallback();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Code Analysis Agent unexpected error for submission {SubmissionId}.", submissionId);
            return Fallback();
        }
    }

    private static string FormatHeuristics(CodeAnalysisHeuristics h)
    {
        var lines = new List<string>
        {
            $"- Total lines: {h.TotalLines} (code: {h.CodeLines}, comments: {h.CommentLines}, blank: {h.BlankLines})",
            $"- Comment ratio: {h.CommentRatio:P0}",
            $"- Average line length: {h.AverageLineLength}",
            $"- Max indent depth: {h.MaxIndentDepth}",
            $"- AI-likelihood heuristic score: {h.AiLikelihoodHeuristic}"
        };
        lines.AddRange(h.Observations.Select(o => "- " + o));
        return string.Join("\n", lines);
    }

    private static IReadOnlyList<CodeCheckerFeedbackItem> Fallback() =>
    [
        new CodeCheckerFeedbackItem(
            FeedbackType.CodeQuality,
            "Automated review is temporarily unavailable. Please check back later.")
    ];

    /// <summary>Internal DTO for the LLM's structured output.</summary>
    private sealed class CodeAnalysisLlmOutput
    {
        [JsonPropertyName("feedbackItems")]
        public List<RawFeedbackItem> FeedbackItems { get; set; } = [];
        
        [JsonPropertyName("aiGenerated")]
        public bool AiGenerated { get; set; }
        
        [JsonPropertyName("aiGeneratedConfidence")]
        public double AiGeneratedConfidence { get; set; }
        
        [JsonPropertyName("aiGeneratedIndicators")]
        public string AiGeneratedIndicators { get; set; } = string.Empty;
    }

    private sealed class RawFeedbackItem
    {
        [JsonPropertyName("feedbackType")]
        public string FeedbackType { get; set; } = string.Empty;
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
