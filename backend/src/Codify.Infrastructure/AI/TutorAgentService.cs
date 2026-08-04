using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.AI;

/// <summary>
/// The agentic Tutor Agent. Uses the OpenAI tool-calling loop: the model
/// decides which tools to call, in what order, and how many — our code only
/// executes whatever tool calls the model requests.
///
/// Loop: send messages + tools → model returns tool_calls → execute them →
/// append results → resend → repeat until model returns final text (no tool
/// calls). Capped at MaxIterations to control cost/latency.
/// </summary>
public class TutorAgentService(
    ILLMClient llmClient,
    ITutorAgentTools tools,
    IPromptLoader promptLoader,
    ILogger<TutorAgentService> logger) : ITutorAgent
{
    private const string PromptFileName = "tutor-agent-system.txt";
    private const int MaxIterations = 5;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILLMClient _llmClient = llmClient;
    private readonly ITutorAgentTools _tools = tools;
    private readonly IPromptLoader _promptLoader = promptLoader;
    private readonly ILogger<TutorAgentService> _logger = logger;

    public async Task<HintResponse> GenerateHintAsync(
        TutorAgentInput input, CancellationToken cancellationToken = default)
    {
        // Build the system prompt (the agentic instructions).
        var systemTemplate = await _promptLoader.LoadAsync(PromptFileName, cancellationToken);

        // Build the initial user message with problem context.
        var userMessage = BuildUserMessage(input);

        // Initialize the conversation messages.
        var messages = new List<LlmMessage>
        {
            new() { Role = "system", Content = systemTemplate },
            new() { Role = "user", Content = userMessage }
        };

        // Get the tool definitions.
        var toolDefs = TutorAgentToolSchemas.GetAll();

        // Track which tools the model calls (for evidence/logging).
        var toolsUsed = new List<string>();
        var iterations = 0;

        // The agent loop: the model decides when to stop calling tools.
        while (iterations < MaxIterations)
        {
            iterations++;
            LlmResponse response;

            try
            {
                response = await _llmClient.CompleteWithToolsAsync(messages, toolDefs, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tutor agent LLM call failed at iteration {Iteration}.", iterations);
                return CreateFallback(input.HintLevel, toolsUsed);
            }

            // If the model returned tool calls, execute them and continue the loop.
            if (response.HasToolCalls && response.ToolCalls.Count > 0)
            {
                // Append the assistant's tool-call message.
                messages.Add(new LlmMessage
                {
                    Role = "assistant",
                    ToolCalls = response.ToolCalls
                });

                // Execute each tool call and append the results.
                foreach (var toolCall in response.ToolCalls)
                {
                    toolsUsed.Add(toolCall.Name);
                    _logger.LogInformation("Tutor agent calling tool: {ToolName} (iteration {Iteration})",
                        toolCall.Name, iterations);

                    var resultJson = await ExecuteToolCallSafelyAsync(toolCall, cancellationToken);
                    messages.Add(new LlmMessage
                    {
                        Role = "tool",
                        ToolCallId = toolCall.Id,
                        Content = resultJson
                    });
                }

                continue; // resend with tool results — the model decides next step
            }

            // The model returned final text (no tool calls) — parse the response.
            var finalText = response.FinalText ?? string.Empty;
            return ParseFinalResponse(finalText, input.HintLevel, toolsUsed);
        }

        // Iteration cap hit — log and return fallback.
        _logger.LogWarning("Tutor agent hit max iterations ({Max}) for problem {ProblemId}. Tools used: {Tools}",
            MaxIterations, input.ProblemId, string.Join(", ", toolsUsed));
        return CreateFallback(input.HintLevel, toolsUsed);
    }

    private async Task<string> ExecuteToolCallSafelyAsync(LlmToolCall toolCall, CancellationToken ct)
    {
        try
        {
            var args = JsonDocument.Parse(toolCall.ArgumentsJson).RootElement;
            return await TutorAgentToolSchemas.ExecuteToolCallAsync(toolCall.Name, args, _tools);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tool execution failed for {ToolName}.", toolCall.Name);
            return $"{{\"error\":\"Tool execution failed: {ex.Message}\"}}";
        }
    }

    private static HintResponse ParseFinalResponse(string rawText, int requestedLevel, List<string> toolsUsed)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return CreateFallback(requestedLevel, toolsUsed);

        try
        {
            var result = JsonSerializer.Deserialize<HintResponse>(rawText, JsonOptions);
            if (result is null || string.IsNullOrWhiteSpace(result.HintText))
                return CreateFallback(requestedLevel, toolsUsed);

            // The agent determines its own hint level (1-3) — clamp for safety.
            result.HintLevel = Math.Clamp(result.HintLevel, HintRequest.MinHintLevel, HintRequest.MaxHintLevel);
            result.ToolsUsed = toolsUsed;
            return result;
        }
        catch (JsonException)
        {
            return CreateFallback(requestedLevel, toolsUsed);
        }
    }

    private static HintResponse CreateFallback(int requestedLevel, List<string> toolsUsed)
    {
        var safeLevel = Math.Clamp(requestedLevel, HintRequest.MinHintLevel, HintRequest.MaxHintLevel);
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
        var conceptTags = input.ConceptTags.Count > 0
            ? string.Join(", ", input.ConceptTags)
            : "None";

        var studentCode = string.IsNullOrWhiteSpace(input.StudentCode) ? "Not provided" : input.StudentCode;
        var language = string.IsNullOrWhiteSpace(input.Language) ? "Not specified" : input.Language;

        return $"""
            A student is requesting a hint for the following problem.

            Problem ID: {input.ProblemId}
            Student ID: {input.UserId}
            Problem title: {input.ProblemTitle}
            Problem statement: {input.ProblemStatement}
            Concept tags: {conceptTags}
            Requested hint level (suggestion only - you determine the actual level): {input.HintLevel}
            Student code (if provided): {studentCode}
            Programming language (if provided): {language}

            Decide which tools you need to call (if any) to gather context before
            writing your hint. You may call zero, one, or multiple tools in any order.
            When you are ready, respond with the structured JSON hint.
            """;
    }
}

