using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.AI;

/// <summary>
/// The Tagging Agent implemented as a STATIC workflow (not agentic tool calling).
/// Pipeline: RAG concept retrieval -> single LLM classification call -> validate
/// the assigned tags against the allowed list. Fired when a problem needs tags.
/// </summary>
public class TaggingAgentService(
    ILLMClient llmClient,
    IPromptLoader promptLoader,
    IKnowledgeBaseSearchService knowledgeBase,
    ILogger<TaggingAgentService> logger) : ITaggingAgent
{
    private const string PromptFileName = "tagging-agent-system.txt";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<TagClassificationResult> ClassifyProblemTagsAsync(
        TaggingAgentInput input, CancellationToken cancellationToken = default)
    {
        if (input.AvailableTags.Count == 0)
        {
            logger.LogWarning("Tagging agent called with no available tags.");
            return EmptyResult("No available tags to classify against.");
        }

        // 1. RAG retrieval: ground the classification in concept knowledge from Chroma.
        var retrievedContext = await RetrieveConceptContextAsync(input, cancellationToken);

        // 2. Render the classification prompt.
        var template = await promptLoader.LoadAsync(PromptFileName, cancellationToken);
        var systemPrompt = PromptTemplate.Render(template, new Dictionary<string, string>
        {
            ["availableTags"] = string.Join(", ", input.AvailableTags),
            ["retrievedContext"] = string.IsNullOrWhiteSpace(retrievedContext) ? "None" : retrievedContext,
            ["problemTitle"] = input.ProblemTitle,
            ["problemStatement"] = input.ProblemStatement
        });

        // 3. Single LLM round-trip.
        string rawResponse;
        try
        {
            rawResponse = await llmClient.CompleteAsync(systemPrompt, "Return only the JSON response.", cancellationToken);
        }
        catch (Exception primaryEx)
        {
            logger.LogError(primaryEx, "Primary LLM failed. Trying Groq fallback...");
            try
            {
                var groqKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(groqKey))
                {
                    logger.LogError("GROQ_API_KEY not set");
                    return EmptyResult("LLM call failed and GROQ_API_KEY not configured.");
                }
                
                rawResponse = await GroqHelper.CallGroqAsync(
                    groqKey, 
                    systemPrompt, 
                    "Return only the JSON response.", 
                    "openai/gpt-oss-20b",
                    cancellationToken);
                logger.LogInformation("✅ Groq fallback succeeded for Tagging Agent");
            }
            catch (Exception groqEx)
            {
                logger.LogError(groqEx, "Groq fallback also failed");
                return EmptyResult("Both primary and fallback LLM calls failed.");
            }
        }

        // 4. Parse and validate against the allowed tag list.
        return ParseAndValidate(rawResponse, input.AvailableTags);
    }

    // ── Private ───────────────────────────────────────────────────

    private async Task<string> RetrieveConceptContextAsync(TaggingAgentInput input, CancellationToken ct)
    {
        try
        {
            var query = $"{input.ProblemTitle} {input.ProblemStatement}";
            var results = await knowledgeBase.SearchAsync(query, conceptTag: null, topK: 3, cancellationToken: ct);
            return string.Join("\n\n", results.Select(r => $"[{r.ConceptTag}] {r.Content}"));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Tagging agent RAG retrieval failed; proceeding without context.");
            return string.Empty;
        }
    }

    private TagClassificationResult ParseAndValidate(string rawResponse, List<string> availableTags)
    {
        try
        {
            var cleaned = rawResponse
                .Replace("```json", string.Empty)
                .Replace("```", string.Empty)
                .Trim();

            var parsed = JsonSerializer.Deserialize<TagClassificationResult>(cleaned, JsonOptions);
            if (parsed is null || parsed.AssignedTags.Count == 0)
                return EmptyResult("No tags returned by the model.");

            // Only keep tags that are in the allowed list (case-insensitive).
            var allowed = availableTags
                .Select(t => t.Trim())
                .ToDictionary(t => t, StringComparer.OrdinalIgnoreCase);

            var validated = parsed.AssignedTags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Where(t => allowed.ContainsKey(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            if (validated.Count == 0)
                return EmptyResult("Model returned tags not in the allowed list.");

            return new TagClassificationResult
            {
                AssignedTags = validated,
                Confidence = Math.Clamp(parsed.Confidence, 0.0, 1.0),
                Reasoning = parsed.Reasoning ?? string.Empty
            };
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Tagging agent JSON parse failed.");
            return EmptyResult("Model returned invalid JSON.");
        }
    }

    private static TagClassificationResult EmptyResult(string reasoning) => new()
    {
        AssignedTags = [],
        Confidence = 0.0,
        Reasoning = reasoning
    };
}
