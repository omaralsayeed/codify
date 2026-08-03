using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.Interfaces;

namespace Codify.Infrastructure.AI;

/// <summary>
/// OpenAI function-calling tool definitions for the .NET-native Analytics Agent.
/// These are sent to the model so it can decide which tools to call.
/// The model — not our C# code — decides which tools to invoke, in what
/// order, and how many.
/// </summary>
public static class AnalyticsAgentToolSchemas
{
    public static List<LlmToolDefinition> GetAll() =>
    [
        new LlmToolDefinition
        {
            Name = "get_submission_history",
            Description = "Get the student's complete submission history with topics and outcomes. Use this as a starting point for any analytics task.",
            ParametersJsonSchema = """{"type":"object","properties":{"userId":{"type":"string","description":"The student's user ID"}},"required":["userId"]}"""
        },
        new LlmToolDefinition
        {
            Name = "aggregate_topic_performance",
            Description = "Calculate per-topic success rates, solved/failed counts, average attempts, and average difficulty. Use this to identify strong and weak topics.",
            ParametersJsonSchema = """{"type":"object","properties":{"userId":{"type":"string","description":"The student's user ID"}},"required":["userId"]}"""
        },
        new LlmToolDefinition
        {
            Name = "detect_weaknesses",
            Description = "Detect recurring weaknesses: common mistakes from submission statuses and feedback, plus topics with low success rates.",
            ParametersJsonSchema = """{"type":"object","properties":{"userId":{"type":"string","description":"The student's user ID"}},"required":["userId"]}"""
        },
        new LlmToolDefinition
        {
            Name = "analyze_trends",
            Description = "Analyze performance over time: improving topics, declining topics, consistency, learning velocity, active/inactive days.",
            ParametersJsonSchema = """{"type":"object","properties":{"userId":{"type":"string","description":"The student's user ID"}},"required":["userId"]}"""
        },
        new LlmToolDefinition
        {
            Name = "generate_tags",
            Description = "Generate structured learning tags: learning stage, overall score, consistency, confidence, weak topics, strong topics, recommended difficulty.",
            ParametersJsonSchema = """{"type":"object","properties":{"userId":{"type":"string","description":"The student's user ID"}},"required":["userId"]}"""
        }
    ];

    public static async Task<string> ExecuteToolCallAsync(
        string toolName,
        JsonElement arguments,
        AnalyticsAgentInput input,
        IAnalyticsAgentTools tools)
    {
        var userId = arguments.TryGetProperty("userId", out var u)
            ? Guid.Parse(u.GetString()!)
            : input.UserId;

        return toolName switch
        {
            "get_submission_history" => JsonSerializer.Serialize(await tools.GetSubmissionHistoryAsync(userId)),
            "aggregate_topic_performance" => JsonSerializer.Serialize(await tools.AggregateTopicPerformanceAsync(userId)),
            "detect_weaknesses" => JsonSerializer.Serialize(await tools.DetectWeaknessesAsync(userId)),
            "analyze_trends" => JsonSerializer.Serialize(await tools.AnalyzeTrendsAsync(userId)),
            "generate_tags" => JsonSerializer.Serialize(await tools.GenerateTagsAsync(userId)),
            _ => """{"error":"Unknown tool"}"""
        };
    }
}
