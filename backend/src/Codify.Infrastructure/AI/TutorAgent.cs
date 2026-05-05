using System.Globalization;
using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.AI;

public class TutorAgent(
    ILLMClient llmClient,
    IPromptLoader promptLoader,
    ILogger<TutorAgent> logger) : ITutorAgent
{
    private const string PromptFileName = "tutor-agent-system.txt";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILLMClient _llmClient = llmClient;
    private readonly IPromptLoader _promptLoader = promptLoader;
    private readonly ILogger<TutorAgent> _logger = logger;

    public async Task<HintResponse> GenerateHintAsync(
        TutorAgentInput input,
        CancellationToken cancellationToken = default)
    {
        var systemTemplate = await _promptLoader.LoadAsync(PromptFileName, cancellationToken);
        var systemPrompt = PromptTemplate.Render(systemTemplate, BuildTemplateValues(input));
        const string userMessage = "Return only the JSON response.";

        string rawResponse;
        try
        {
            rawResponse = await _llmClient.CompleteAsync(systemPrompt, userMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tutor agent call failed for problem {ProblemId}.", input.ProblemId);
            return CreateFallback(input.HintLevel);
        }

        try
        {
            var result = JsonSerializer.Deserialize<HintResponse>(rawResponse, JsonOptions);
            if (!IsValid(result))
            {
                _logger.LogWarning("Tutor agent returned invalid JSON payload for problem {ProblemId}.", input.ProblemId);
                return CreateFallback(input.HintLevel);
            }

            return result!;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Tutor agent JSON parse failed for problem {ProblemId}.", input.ProblemId);
            return CreateFallback(input.HintLevel);
        }
    }

    private static bool IsValid(HintResponse? response)
    {
        if (response is null)
            return false;

        if (string.IsNullOrWhiteSpace(response.HintText))
            return false;

        return response.HintLevel >= HintRequest.MinHintLevel
            && response.HintLevel <= HintRequest.MaxHintLevel;
    }

    private static HintResponse CreateFallback(int hintLevel)
    {
        var safeLevel = Math.Clamp(hintLevel, HintRequest.MinHintLevel, HintRequest.MaxHintLevel);
        return new HintResponse
        {
            HintText = "Try reviewing the problem constraints. They often hint at the right approach.",
            HintLevel = safeLevel,
            FollowUpQuestion = null,
            HasMoreHints = true
        };
    }

    private static IReadOnlyDictionary<string, string> BuildTemplateValues(TutorAgentInput input)
    {
        var previousHints = input.PreviousHints.Count > 0
            ? string.Join("\n", input.PreviousHints.Select(hint => "- " + hint))
            : "None";

        var conceptTags = input.ConceptTags.Count > 0
            ? string.Join(", ", input.ConceptTags)
            : "None";

        return new Dictionary<string, string>
        {
            ["problemTitle"] = input.ProblemTitle,
            ["problemStatement"] = input.ProblemStatement,
            ["conceptTags"] = conceptTags,
            ["retrievedContext"] = string.IsNullOrWhiteSpace(input.RetrievedContext)
                ? "None"
                : input.RetrievedContext,
            ["hintLevel"] = input.HintLevel.ToString(CultureInfo.InvariantCulture),
            ["previousHints"] = previousHints,
            ["attemptCount"] = input.AttemptCount.ToString(CultureInfo.InvariantCulture)
        };
    }
}
