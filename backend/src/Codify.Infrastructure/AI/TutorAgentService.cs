using System.Diagnostics;
using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Codify.Infrastructure.AI;

/// <summary>
/// The agentic Tutor Agent. Uses the OpenAI tool-calling loop: the model
/// decides which tools to call, in what order, and how many — this code only
/// executes whatever tool calls the model requests, appends the results, and
/// resends until the model returns a final JSON hint.
///
/// Loop: send messages + tools → model returns tool_calls → execute them →
/// append results → resend → repeat until the model returns final text.
/// Capped at MaxIterations to control cost/latency; falls back safely on
/// errors, invalid JSON, or hitting the cap.
/// </summary>
public class TutorAgentService(
    ILLMClient llmClient,
    ITutorAgentTools tools,
    IPromptLoader promptLoader,
    IOptions<OpenAiOptions> options,
    ILogger<TutorAgentService> logger) : ITutorAgent
{
    private readonly OpenAiOptions _options = options.Value;
    private const string PromptFileName = "tutor-agent-system.txt";
    private const int MaxIterations = 5;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<HintResponse> GenerateHintAsync(
        TutorAgentInput input, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var model = ChooseModel(input);
        logger.LogInformation("Tutor agent using model {Model} for problem {ProblemId} (attempt={Attempt}, hintLevel={HintLevel})",
            model, input.ProblemId, input.AttemptCount, input.HintLevel);

        var systemTemplate = await promptLoader.LoadAsync(PromptFileName, cancellationToken);
        var userMessage = BuildUserMessage(input);

        var messages = new List<LlmMessage>
        {
            new() { Role = "system", Content = systemTemplate },
            new() { Role = "user", Content = userMessage }
        };

        var toolDefs = TutorAgentToolSchemas.GetAll();
        var toolsUsed = new List<string>();
        var iterations = 0;
        string? lastModelUsed = null;
        int totalTokensAccumulated = 0;

        HintResponse? result = null;

        while (iterations < MaxIterations)
        {
            iterations++;
            LlmResponse response;

            try
            {
                response = await llmClient.CompleteWithToolsAsync(messages, toolDefs, model, cancellationToken);
                lastModelUsed = response.ModelUsed ?? model;
                totalTokensAccumulated += response.TotalTokens ?? 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tutor agent LLM call failed for problem {ProblemId}.", input.ProblemId);
                break;
            }

            if (response.HasToolCalls)
            {
                messages.Add(new LlmMessage { Role = "assistant", ToolCalls = response.ToolCalls });

                foreach (var toolCall in response.ToolCalls)
                {
                    toolsUsed.Add(toolCall.Name);
                    logger.LogInformation("Tutor agent calling tool: {ToolName}", toolCall.Name);
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
                result = ParseFinalResponse(response.FinalText ?? string.Empty, input.HintLevel, toolsUsed);
                break;
            }
        }

        stopwatch.Stop();
        var latencyMs = (int)stopwatch.ElapsedMilliseconds;

        if (result is null)
        {
            logger.LogWarning("Tutor agent hit max iterations ({Max}) for problem {ProblemId}. Tools used: {Tools}",
                MaxIterations, input.ProblemId, string.Join(", ", toolsUsed));
            result = CreateFallback(input.HintLevel, toolsUsed);
        }

        result.ModelUsed = lastModelUsed;
        result.TotalTokens = totalTokensAccumulated;
        result.LatencyMs = latencyMs;

        logger.LogInformation("Tutor agent completed in {LatencyMs}ms using {Model} ({Iterations} iterations, {Tokens} tokens)",
            latencyMs, lastModelUsed, iterations, totalTokensAccumulated);

        return result;
    }

    // ── Private ───────────────────────────────────────────────────

    private async Task<string> ExecuteToolCallSafelyAsync(
        LlmToolCall toolCall, TutorAgentInput input, CancellationToken ct)
    {
        try
        {
            var args = JsonDocument.Parse(toolCall.ArgumentsJson).RootElement;
            return await TutorAgentToolSchemas.ExecuteToolCallAsync(toolCall.Name, args, input, tools);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Tutor tool execution failed for {ToolName}.", toolCall.Name);
            return $"{{\"error\":\"Tool execution failed: {ex.Message}\"}}";
        }
    }

    private HintResponse ParseFinalResponse(string rawText, int suggestedLevel, List<string> toolsUsed)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return CreateFallback(suggestedLevel, toolsUsed);

        try
        {
            // Strip markdown fences if the model wrapped the JSON.
            var cleaned = rawText.Replace("```json", string.Empty).Replace("```", string.Empty).Trim();

            var result = JsonSerializer.Deserialize<HintResponse>(cleaned, JsonOptions);
            if (result is null || string.IsNullOrWhiteSpace(result.HintText))
            {
                logger.LogWarning("Tutor agent returned invalid/empty hint payload.");
                return CreateFallback(suggestedLevel, toolsUsed);
            }

            // Clamp the agent's own assessed hint level to the valid range.
            result.HintLevel = Math.Clamp(result.HintLevel, HintRequest.MinHintLevel, HintRequest.MaxHintLevel);
            result.ToolsUsed = toolsUsed;
            return result;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Tutor agent JSON parse failed.");
            return CreateFallback(suggestedLevel, toolsUsed);
        }
    }

    private static HintResponse CreateFallback(int hintLevel, List<string> toolsUsed)
    {
        var safeLevel = Math.Clamp(hintLevel, HintRequest.MinHintLevel, HintRequest.MaxHintLevel);
        return new HintResponse
        {
            HintText = "Try reviewing the problem constraints. They often hint at the right approach.",
            HintLevel = safeLevel,
            FollowUpQuestion = null,
            HasMoreHints = true,
            ToolsUsed = toolsUsed,
            ReasoningSummary = "Fallback: LLM call failed or returned invalid response."
        };
    }

    private static string BuildUserMessage(TutorAgentInput input)
    {
        var conceptTags = input.ConceptTags.Count > 0 ? string.Join(", ", input.ConceptTags) : "None";
        var previousHints = input.PreviousHints.Count > 0
            ? string.Join("\n", input.PreviousHints.Select(h => "- " + h))
            : "None";
        var codeBlock = string.IsNullOrWhiteSpace(input.StudentCode)
            ? "No code provided."
            : $"```\n{input.StudentCode}\n```";

        return $"""
            Problem title: {input.ProblemTitle}
            Problem statement: {input.ProblemStatement}
            Concept tags: {conceptTags}
            Suggested hint level (you decide the real level): {input.HintLevel}
            Previous hints given:
            {previousHints}
            Student attempt count: {input.AttemptCount}
            Last submission status: {(input.LastSubmissionStatus?.ToString() ?? "None")}

            Student code:
            {codeBlock}

            Decide which tools you need (if any), gather context, then return the final JSON hint.
            """;
    }

    /// <summary>
    /// Decides which model to use based on the student's attempt count and hint level.
    /// Routine cases use gpt-4o-mini (cheap, fast); complex cases escalate to gpt-4o.
    /// </summary>
    private string ChooseModel(TutorAgentInput input)
    {
        if (input.AttemptCount >= _options.EscalationAttemptThreshold
            && input.HintLevel >= _options.EscalationHintLevelThreshold)
        {
            logger.LogInformation(
                "Escalating to {EscalationModel}: attempt={Attempt} >= {AttemptThreshold} AND hintLevel={HintLevel} >= {HintLevelThreshold}",
                _options.EscalationModel, input.AttemptCount, _options.EscalationAttemptThreshold,
                input.HintLevel, _options.EscalationHintLevelThreshold);
            return _options.EscalationModel;
        }

        return string.IsNullOrWhiteSpace(_options.Model)
            ? OpenAiOptions.DefaultModel
            : _options.Model;
    }
}
