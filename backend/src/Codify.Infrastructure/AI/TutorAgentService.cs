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
        logger.LogInformation(
            "🎯 Tutor agent starting: Model={Model}, ProblemId={ProblemId}, AttemptCount={Attempt}, HintLevel={HintLevel}",
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

        logger.LogInformation("🔄 Starting agent loop (max {MaxIterations} iterations)...", MaxIterations);

        while (iterations < MaxIterations)
        {
            iterations++;
            logger.LogInformation("🔄 Iteration {Iteration}/{Max}...", iterations, MaxIterations);
            
            LlmResponse response;

            try
            {
                // Create linked cancellation token with 15-second timeout per LLM call
                using var callCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                callCts.CancelAfter(TimeSpan.FromSeconds(15));
                
                // Use dynamic max_tokens: lower for tool-calling turns, higher for final response
                // Start with conservative 800 tokens - will increase to 2000 if no tool calls returned
                var maxTokens = 800;
                
                response = await llmClient.CompleteWithToolsAsync(messages, toolDefs, model, maxTokens, callCts.Token);
                lastModelUsed = response.ModelUsed ?? model;
                totalTokensAccumulated += response.TotalTokens ?? 0;
                
                logger.LogInformation(
                    "✅ LLM call successful. TokensThisCall={Tokens}, TotalSoFar={TotalTokens}, MaxTokensUsed={MaxTokens}",
                    response.TotalTokens ?? 0, totalTokensAccumulated, maxTokens);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    "⏱️  LLM call timed out after 15s at iteration {Iteration}/{Max}. ProblemId={ProblemId}",
                    iterations, MaxIterations, input.ProblemId);
                break; // Fall back to default hint
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "❌ Tutor agent LLM call failed at iteration {Iteration}. ProblemId={ProblemId}, Error: {ErrorType} - {ErrorMessage}", 
                    iterations, input.ProblemId, ex.GetType().Name, ex.Message);
                break;
            }

            if (response.HasToolCalls)
            {
                logger.LogInformation("🔧 Agent requested {Count} tool(s): {Tools}", 
                    response.ToolCalls.Count, 
                    string.Join(", ", response.ToolCalls.Select(t => t.Name)));
                
                messages.Add(new LlmMessage { Role = "assistant", ToolCalls = response.ToolCalls });

                // Execute all tools in parallel for better performance
                logger.LogInformation("⚡ Executing {Count} tool(s) in parallel...", response.ToolCalls.Count);
                var toolTasks = response.ToolCalls.Select(async toolCall =>
                {
                    toolsUsed.Add(toolCall.Name);
                    logger.LogInformation("⚙️  Executing tool: {ToolName}", toolCall.Name);
                    var stopwatch = Stopwatch.StartNew();
                    var resultJson = await ExecuteToolCallSafelyAsync(toolCall, input, cancellationToken);
                    stopwatch.Stop();
                    logger.LogInformation("✅ Tool {ToolName} completed in {Ms}ms, result length: {Length}", 
                        toolCall.Name, stopwatch.ElapsedMilliseconds, resultJson.Length);
                    return (toolCall.Id, resultJson);
                }).ToList();

                var results = await Task.WhenAll(toolTasks);
                
                logger.LogInformation("✅ All {Count} tools completed", results.Length);

                foreach (var (toolCallId, resultJson) in results)
                {
                    messages.Add(new LlmMessage
                    {
                        Role = "tool",
                        ToolCallId = toolCallId,
                        Content = resultJson
                    });
                }
            }
            else
            {
                // No tool calls - this is the final response, but it might have been truncated
                // due to low max_tokens. If response looks incomplete, retry with higher limit.
                var needsRetry = false;
                
                if (string.IsNullOrWhiteSpace(response.FinalText))
                {
                    logger.LogWarning("⚠️  Agent returned empty final response, will retry with higher max_tokens");
                    needsRetry = true;
                }
                else if (response.FinalText.Length < 50 && !response.FinalText.Contains("}"))
                {
                    logger.LogWarning("⚠️  Agent returned suspiciously short response ({Length} chars), will retry with higher max_tokens", 
                        response.FinalText.Length);
                    needsRetry = true;
                }
                
                if (needsRetry && iterations < MaxIterations)
                {
                    logger.LogInformation("🔄 Retrying with max_tokens=2000 for complete final response...");
                    
                    // Remove the last assistant message if it exists
                    if (messages.Count > 0 && messages[^1].Role == "assistant")
                        messages.RemoveAt(messages.Count - 1);
                    
                    try
                    {
                        using var callCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        callCts.CancelAfter(TimeSpan.FromSeconds(15));
                        
                        response = await llmClient.CompleteWithToolsAsync(messages, toolDefs, model, maxTokens: 2000, callCts.Token);
                        lastModelUsed = response.ModelUsed ?? model;
                        totalTokensAccumulated += response.TotalTokens ?? 0;
                        iterations++; // Count this as an iteration
                        
                        logger.LogInformation(
                            "✅ Retry successful. TokensThisCall={Tokens}, TotalSoFar={TotalTokens}",
                            response.TotalTokens ?? 0, totalTokensAccumulated);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        logger.LogWarning("⏱️  Retry LLM call timed out after 15s");
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "❌ Retry LLM call failed: {ErrorType} - {ErrorMessage}", 
                            ex.GetType().Name, ex.Message);
                        break;
                    }
                }
                
                logger.LogInformation("🏁 Agent returned final response. Response details: HasToolCalls={HasToolCalls}, ToolCallsCount={ToolCallsCount}, FinalTextLength={FinalTextLength}", 
                    response.HasToolCalls, response.ToolCalls?.Count ?? 0, response.FinalText?.Length ?? 0);
                logger.LogInformation("📄 Final text content: {FinalText}", response.FinalText ?? "(null)");
                
                result = ParseFinalResponse(response.FinalText ?? string.Empty, input.HintLevel, toolsUsed);
                break;
            }
        }

        stopwatch.Stop();
        var latencyMs = (int)stopwatch.ElapsedMilliseconds;

        if (result is null)
        {
            logger.LogWarning(
                "⚠️  Tutor agent hit max iterations ({Max}) without final response. ProblemId={ProblemId}, Tools used: {Tools}",
                MaxIterations, input.ProblemId, string.Join(", ", toolsUsed));
            result = CreateFallback(input.HintLevel, toolsUsed);
        }

        result.ModelUsed = lastModelUsed;
        result.TotalTokens = totalTokensAccumulated;
        result.LatencyMs = latencyMs;

        logger.LogInformation(
            "🎉 Tutor agent completed: LatencyMs={LatencyMs}, Model={Model}, Iterations={Iterations}, Tokens={Tokens}, " +
            "HintLevel={HintLevel}, ToolsUsed={ToolsUsed}, IsFallback={IsFallback}",
            latencyMs, lastModelUsed, iterations, totalTokensAccumulated, 
            result.HintLevel, string.Join(", ", toolsUsed), result.ReasoningSummary?.Contains("Fallback") ?? false);

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
            // The API might return tool calls + reasoning + JSON all in one response
            // Extract just the JSON object from the text
            var jsonStart = rawText.IndexOf('{');
            var jsonEnd = rawText.LastIndexOf('}');

            if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
            {
                logger.LogWarning("Tutor agent response doesn't contain JSON object. Response: {Response}", 
                    rawText.Length > 200 ? rawText.Substring(0, 200) + "..." : rawText);
                return CreateFallback(suggestedLevel, toolsUsed);
            }

            var jsonText = rawText.Substring(jsonStart, jsonEnd - jsonStart + 1);
            logger.LogInformation("📄 [DEBUG] Extracted JSON from response:\n{Json}", jsonText);

            var result = JsonSerializer.Deserialize<HintResponse>(jsonText, JsonOptions);
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
            logger.LogWarning(ex, "Tutor agent JSON parse failed. Raw text length: {Length}", rawText.Length);
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
