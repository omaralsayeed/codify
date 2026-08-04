using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.AI;

/// <summary>
/// The .NET-native Code Analysis Agent. Uses the OpenAI tool-calling loop:
/// the model decides which tools to call, in what order, and how many — our
/// code only executes whatever tool calls the model requests.
///
/// Loop: send messages + tools → model returns tool_calls → execute them →
/// append results → resend → repeat until model returns final text (no tool
/// calls). Capped at MaxIterations to control cost/latency.
/// </summary>
public class CodeAnalysisAgentService(
    ILLMClient llmClient,
    ICodeAnalysisAgentTools tools,
    IPromptLoader promptLoader,
    ILogger<CodeAnalysisAgentService> logger) : ICodeAnalysisAgent
{
    private const string PromptFileName = "code-analysis-agent-system.txt";
    private const int MaxIterations = 5;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<CodeAnalysisResult> AnalyzeAsync(
        CodeAnalysisAgentInput input, CancellationToken cancellationToken = default)
    {
        var systemTemplate = await promptLoader.LoadAsync(PromptFileName, cancellationToken);
        var userMessage = BuildUserMessage(input);

        var messages = new List<LlmMessage>
        {
            new() { Role = "system", Content = systemTemplate },
            new() { Role = "user", Content = userMessage }
        };

        var toolDefs = CodeAnalysisAgentToolSchemas.GetAll();
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
                logger.LogError(ex, "Code analysis agent LLM call failed at iteration {Iteration}.", iterations);
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
                    logger.LogInformation("Code analysis agent calling tool: {ToolName}", toolCall.Name);
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

        logger.LogWarning("Code analysis agent hit max iterations ({Max}) for submission {SubmissionId}. Tools used: {Tools}",
            MaxIterations, input.SubmissionId, string.Join(", ", toolsUsed));
        return CreateFallback(toolsUsed);
    }

    private async Task<string> ExecuteToolCallSafelyAsync(LlmToolCall toolCall, CodeAnalysisAgentInput input, CancellationToken ct)
    {
        try
        {
            var args = JsonDocument.Parse(toolCall.ArgumentsJson).RootElement;
            return await CodeAnalysisAgentToolSchemas.ExecuteToolCallAsync(toolCall.Name, args, input, tools);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Tool execution failed for {ToolName}.", toolCall.Name);
            return $"{{\"error\":\"Tool execution failed: {ex.Message}\"}}";
        }
    }

    private static CodeAnalysisResult ParseFinalResponse(string rawText, List<string> toolsUsed)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return CreateFallback(toolsUsed);

        try
        {
            var result = JsonSerializer.Deserialize<CodeAnalysisResult>(rawText, JsonOptions);
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

    private static CodeAnalysisResult CreateFallback(List<string> toolsUsed) => new()
    {
        Verdict = "Analysis unavailable",
        OverallMessage = "The code analysis agent could not produce a structured response. Please try again.",
        TimeComplexity = "Unknown",
        SpaceComplexity = "Unknown",
        IntegrityFlag = false,
        ToolsUsed = toolsUsed,
        ReasoningSummary = "Fallback: LLM call failed or returned invalid response."
    };

    private static string BuildUserMessage(CodeAnalysisAgentInput input)
    {
        var codePreview = input.Code.Length > 2000
            ? input.Code[..2000] + "\n... (truncated)"
            : input.Code;

        return $"""
            Analyze the following submitted code.

            Problem ID: {input.ProblemId}
            Problem title: {input.ProblemTitle}
            Problem statement: {input.ProblemStatement}
            Constraints: {input.Constraints}
            Difficulty: {input.Difficulty}
            Time limit (ms): {input.TimeLimitMs}
            Memory limit (MB): {input.MemoryLimitMb}
            Language: {input.Language}
            User ID: {input.UserId}
            Submission ID: {input.SubmissionId}

            Code:
            ```{input.Language}
            {codePreview}
            ```

            Decide which tools you need to call (if any) to gather context before
            producing your analysis. You may call zero, one, or multiple tools in any order.
            When you are ready, respond with the structured JSON analysis.
            """;
    }
}


