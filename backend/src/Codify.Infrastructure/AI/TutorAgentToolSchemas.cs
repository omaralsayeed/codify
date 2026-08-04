using System.Text.Json;
using Codify.Application.Interfaces;

namespace Codify.Infrastructure.AI;

/// <summary>
/// OpenAI function-calling tool definitions for the agentic Tutor Agent.
/// These are sent to the model so it can decide which tools to call.
/// The model — not our C# code — decides which tools to invoke, in what
/// order, and how many.
/// </summary>
public static class TutorAgentToolSchemas
{
    public static List<LlmToolDefinition> GetAll() =>
    [
        new LlmToolDefinition
        {
            Name = "get_attempt_history",
            Description = "Get the student's attempt history for a problem: number of attempts, past submission statuses, previous hint levels already given, and timestamps. Use this to judge how stuck the student really is instead of trusting a client-supplied hint level.",
            ParametersJsonSchema = """{"type":"object","properties":{"studentId":{"type":"string","description":"The student's user ID"},"problemId":{"type":"string","description":"The problem ID"}},"required":["studentId","problemId"]}"""
        },
        new LlmToolDefinition
        {
            Name = "search_knowledge_base",
            Description = "Search the DSA knowledge base for concept-level explanations, patterns, and common mistakes. Use this when the student seems to be missing understanding of an underlying concept (not just stuck on this specific problem).",
            ParametersJsonSchema = """{"type":"object","properties":{"query":{"type":"string","description":"The search query (e.g. 'hash map two sum', 'recursion base case')"},"conceptTag":{"type":"string","description":"Optional: filter by a specific concept tag name"}},"required":["query"]}"""
        },
        new LlmToolDefinition
        {
            Name = "check_partial_code",
            Description = "Get lightweight static observations about partial code the student submitted: syntax validity, rough structure (loops, recursion, base cases, hash map usage). NOT a full code review. Use this if the student included code with their request to tailor the hint to what they've actually written.",
            ParametersJsonSchema = """{"type":"object","properties":{"code":{"type":"string","description":"The student's partial code"},"language":{"type":"string","description":"The programming language (Python or CSharp)"}},"required":["code","language"]}"""
        },
        new LlmToolDefinition
        {
            Name = "get_previous_hints",
            Description = "Get the exact text and levels of hints already given to this student for this problem. Use this to avoid repeating guidance and to judge how much more specific the next hint should be.",
            ParametersJsonSchema = """{"type":"object","properties":{"studentId":{"type":"string","description":"The student's user ID"},"problemId":{"type":"string","description":"The problem ID"}},"required":["studentId","problemId"]}"""
        }
    ];

    /// <summary>Execute a single tool call by name with parsed arguments.</summary>
    public static async Task<string> ExecuteToolCallAsync(
        string toolName,
        JsonElement arguments,
        ITutorAgentTools tools)
    {
        return toolName switch
        {
            "get_attempt_history" => await ExecuteGetAttemptHistoryAsync(arguments, tools),
            "search_knowledge_base" => await ExecuteSearchKnowledgeBaseAsync(arguments, tools),
            "check_partial_code" => await ExecuteCheckPartialCodeAsync(arguments, tools),
            "get_previous_hints" => await ExecuteGetPreviousHintsAsync(arguments, tools),
            _ => """{"error":"Unknown tool"}"""
        };
    }

    private static async Task<string> ExecuteGetAttemptHistoryAsync(JsonElement args, ITutorAgentTools tools)
    {
        var studentId = Guid.Parse(args.GetProperty("studentId").GetString()!);
        var problemId = Guid.Parse(args.GetProperty("problemId").GetString()!);
        var result = await tools.GetAttemptHistoryAsync(studentId, problemId);
        return JsonSerializer.Serialize(result);
    }

    private static async Task<string> ExecuteSearchKnowledgeBaseAsync(JsonElement args, ITutorAgentTools tools)
    {
        var query = args.GetProperty("query").GetString()!;
        var conceptTag = args.TryGetProperty("conceptTag", out var tag) ? tag.GetString() : null;
        var result = await tools.SearchKnowledgeBaseAsync(query, conceptTag);
        return JsonSerializer.Serialize(result);
    }

    private static async Task<string> ExecuteCheckPartialCodeAsync(JsonElement args, ITutorAgentTools tools)
    {
        var code = args.GetProperty("code").GetString()!;
        var language = args.GetProperty("language").GetString()!;
        var result = await tools.CheckPartialCodeAsync(code, language);
        return JsonSerializer.Serialize(result);
    }

    private static async Task<string> ExecuteGetPreviousHintsAsync(JsonElement args, ITutorAgentTools tools)
    {
        var studentId = Guid.Parse(args.GetProperty("studentId").GetString()!);
        var problemId = Guid.Parse(args.GetProperty("problemId").GetString()!);
        var result = await tools.GetPreviousHintsAsync(studentId, problemId);
        return JsonSerializer.Serialize(result);
    }
}
