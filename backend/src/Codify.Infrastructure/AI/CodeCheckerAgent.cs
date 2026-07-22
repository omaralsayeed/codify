using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.Interfaces;
using Codify.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.AI;

/// <summary>
/// Calls the LLM to review submitted code and return structured feedback.
///
/// The agent is asked to return a JSON array so we can map each item
/// to a FeedbackRecord with a specific FeedbackType.
///
/// Example LLM response:
/// [
///   { "feedbackType": "CodeQuality",   "message": "Use meaningful variable names." },
///   { "feedbackType": "Optimization",  "message": "Consider using a hash set here." }
/// ]
/// </summary>
public class CodeCheckerAgent(
    ILLMClient llmClient,
    ILogger<CodeCheckerAgent> logger) : ICodeCheckerAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<CodeCheckerFeedbackItem>> AnalyzeAsync(
        CodeCheckerAgentInput input,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(input);
        const string userMessage = "Return only the JSON array. No explanation, no markdown.";

        string rawResponse;
        try
        {
            rawResponse = await llmClient.CompleteAsync(systemPrompt, userMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "CodeCheckerAgent LLM call failed for submission {SubmissionId}.",
                input.SubmissionId);

            return Fallback();
        }

        try
        {
            // Strip markdown fences if the model wraps the JSON
            var cleaned = rawResponse
                .Replace("```json", string.Empty)
                .Replace("```", string.Empty)
                .Trim();

            var items = JsonSerializer.Deserialize<List<CodeCheckerRawItem>>(cleaned, JsonOptions);

            if (items is null || items.Count == 0)
            {
                logger.LogWarning(
                    "CodeCheckerAgent returned empty payload for submission {SubmissionId}.",
                    input.SubmissionId);

                return Fallback();
            }

            // Parse each item — skip any with unknown FeedbackType so we never crash
            var result = new List<CodeCheckerFeedbackItem>();
            foreach (var item in items)
            {
                if (!Enum.TryParse<FeedbackType>(item.FeedbackType, ignoreCase: true, out var feedbackType))
                {
                    logger.LogWarning(
                        "CodeCheckerAgent returned unknown FeedbackType '{Type}' — skipping.",
                        item.FeedbackType);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(item.Message))
                    result.Add(new CodeCheckerFeedbackItem(feedbackType, item.Message.Trim()));
            }

            return result.Count > 0 ? result : Fallback();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex,
                "CodeCheckerAgent JSON parse failed for submission {SubmissionId}.",
                input.SubmissionId);

            return Fallback();
        }
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static string BuildSystemPrompt(CodeCheckerAgentInput input) =>
        $$"""
        You are an expert code reviewer for an online coding platform.

        Problem: {{input.ProblemTitle}}
        Description: {{input.ProblemStatement}}
        Language: {{input.Language}}

        Student code:
        ```
        {{input.Code}}
        ```

        Review the code and return a JSON array with 2-3 feedback items.
        Each item must have exactly these two fields:
          - "feedbackType": one of "CodeQuality", "Optimization", "IntegrityFlag"
          - "message": a concise, constructive comment (max 200 characters)

        Use "IntegrityFlag" only if the code looks copied or plagiarised.
        Return ONLY the JSON array. No explanation, no markdown fences.

        Example:
        [
          {"feedbackType": "CodeQuality", "message": "Use descriptive variable names instead of x and y."},
          {"feedbackType": "Optimization", "message": "A hash set would reduce lookup time from O(n) to O(1)."}
        ]
        """;

    /// <summary>
    /// Safe fallback used when the LLM fails or returns unparseable output.
    /// Ensures the caller always gets at least one FeedbackRecord saved.
    /// </summary>
    private static IReadOnlyList<CodeCheckerFeedbackItem> Fallback() =>
    [
        new CodeCheckerFeedbackItem(
            FeedbackType.CodeQuality,
            "Automated review is temporarily unavailable. Please check back later.")
    ];

    // Internal DTO for JSON deserialization only — not exposed outside this class
    private sealed class CodeCheckerRawItem
    {
        public string FeedbackType { get; set; } = string.Empty;
        public string Message      { get; set; } = string.Empty;
    }
}
