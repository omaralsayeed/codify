using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.AI;

/// <summary>
/// The .NET-native Analytics / Tagging Agent. Uses the OpenAI tool-calling loop:
/// the model decides which tools to call, in what order, and how many — our
/// code only executes whatever tool calls the model requests.
///
/// Loop: send messages + tools → model returns tool_calls → execute them →
/// append results → resend → repeat until model returns final text (no tool
/// calls). Capped at MaxIterations to control cost/latency.
/// </summary>
public class AnalyticsAgentService(
    ILLMClient llmClient,
    IAnalyticsAgentTools tools,
    IPromptLoader promptLoader,
    ILogger<AnalyticsAgentService> logger) : IAnalyticsAgent
{
    private const string PromptFileName = "analytics-agent-system.txt";
    private const int MaxIterations = 5;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<AnalyticsResult> AnalyzeAsync(
        AnalyticsAgentInput input, CancellationToken cancellationToken = default)
    {
        var systemTemplate = await promptLoader.LoadAsync(PromptFileName, cancellationToken);
        var userMessage = BuildUserMessage(input);

        var messages = new List<LlmMessage>
        {
            new() { Role = "system", Content = systemTemplate },
            new() { Role = "user", Content = userMessage }
        };

        var toolDefs = AnalyticsAgentToolSchemas.GetAll();
        var toolsUsed = new List<string>();
        var iterations = 0;

        while (iterations < MaxIterations)
        {
            iterations++;
            LlmResponse response;

            try
            {
                response = await llmClient.CompleteWithToolsAsync(messages, toolDefs, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Analytics agent LLM call failed at iteration {Iteration}.", iterations);
                return CreateFallback(toolsUsed);
            }

            if (response.HasToolCalls && response.ToolCalls.Count > 0)
            {
                messages.Add(new LlmMessage
                {
                    Role = "assistant",
                    ToolCalls = response.ToolCalls
                });

                foreach (var toolCall in response.ToolCalls)
                {
                    toolsUsed.Add(toolCall.Name);
                    logger.LogInformation("Analytics agent calling tool: {ToolName}", toolCall.Name);
                    var resultJson = await ExecuteToolCallSafelyAsync(toolCall, input, cancellationToken);
                    messages.Add(new LlmMessage
                    {
                        Role = "tool",
                        ToolCallId = toolCall.Id,
                        Content = resultJson
                    });
                }
            }
            else
            {
                return ParseFinalResponse(response.FinalText ?? string.Empty, toolsUsed);
            }
        }

        logger.LogWarning("Analytics agent hit max iterations ({Max}) for user {UserId}. Tools used: {Tools}",
            MaxIterations, input.UserId, string.Join(", ", toolsUsed));
        return CreateFallback(toolsUsed);
    }

    private async Task<string> ExecuteToolCallSafelyAsync(LlmToolCall toolCall, AnalyticsAgentInput input, CancellationToken ct)
    {
        try
        {
            var args = JsonDocument.Parse(toolCall.ArgumentsJson).RootElement;
            return await AnalyticsAgentToolSchemas.ExecuteToolCallAsync(toolCall.Name, args, input, tools);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Tool execution failed for {ToolName}.", toolCall.Name);
            return $"{{\"error\":\"Tool execution failed: {ex.Message}\"}}";
        }
    }

    private static AnalyticsResult ParseFinalResponse(string rawText, List<string> toolsUsed)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return CreateFallback(toolsUsed);

        try
        {
            var result = JsonSerializer.Deserialize<AnalyticsResult>(rawText, JsonOptions);
            if (result is null)
                return CreateFallback(toolsUsed);

            result.ToolsUsed = toolsUsed;
            return result;
        }
        catch (JsonException)
        {
            return CreateFallback(toolsUsed);
        }
    }

    private static AnalyticsResult CreateFallback(List<string> toolsUsed) => new()
    {
        Summary = "Analytics could not be completed at this time. Please try again later.",
        LearningStage = "Beginner",
        Consistency = "Low",
        RecommendedProblemDifficulty = "Easy",
        ToolsUsed = toolsUsed,
        ReasoningSummary = "Fallback: LLM call failed or returned invalid response."
    };

    private static string BuildUserMessage(AnalyticsAgentInput input)
    {
        var topics = string.Join(", ", input.AvailableTopics);
        return $"""
            Analyze the learning profile for user {input.UserId}.

            Available topics in the platform: {topics}
            Number of prior hints requested by this user: {input.HintCount}

            Decide which tools you need to call (if any) to gather context before
            producing the structured analytics profile. You may call zero, one, or
            multiple tools in any order.

            When you are ready, respond with the structured JSON analytics profile.
            """;
    }
}


