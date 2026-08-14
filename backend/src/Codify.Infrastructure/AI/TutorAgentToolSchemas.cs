using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.Interfaces;

namespace Codify.Infrastructure.AI;

/// <summary>
/// OpenAI function-calling tool definitions for the agentic Tutor Agent, plus
/// the dispatcher that executes a model-requested tool call. The model — not
/// this code — decides which tools to invoke and in what order.
/// </summary>
public static class TutorAgentToolSchemas
{
    public static List<LlmToolDefinition> GetAll() =>
    [
        new LlmToolDefinition
        {
            Name = "get_attempt_history",
            Description = "Get how many times the student has attempted this problem, their past submission statuses, previous hint levels, and timestamps. Use this to judge how stuck the student really is instead of trusting the client-supplied hint level.",
            ParametersJsonSchema = """{"type":"object","properties":{"studentId":{"type":"string","description":"The student's user ID"},"problemId":{"type":"string","description":"The problem ID"}},"required":["studentId","problemId"]}"""
        },
        new LlmToolDefinition
        {
            Name = "search_knowledge_base",
            Description = "Search the DSA concept knowledge base (Chroma) for grounding material. Use this when the student seems to be missing understanding of an underlying concept (not just stuck on this specific problem). Returns relevant concept-doc chunks.",
            ParametersJsonSchema = """{"type":"object","properties":{"query":{"type":"string","description":"Natural language search query, e.g. 'hash map lookups'"},"conceptTag":{"type":"string","description":"Optional concept tag to narrow the search"}},"required":["query"]}"""
        },
        new LlmToolDefinition
        {
            Name = "check_partial_code",
            Description = "Run lightweight static observations on the student's submitted code (loop/recursion/base-case/syntax heuristics). Use this when the student included code, to tailor the hint to what they actually wrote. Not a full code review.",
            ParametersJsonSchema = """{"type":"object","properties":{"code":{"type":"string","description":"The student's code"},"language":{"type":"string","description":"Programming language, e.g. 'Python' or 'CSharp'"}},"required":["code","language"]}"""
        },
        new LlmToolDefinition
        {
            Name = "get_previous_hints",
            Description = "Get the exact text of hints already given to this student for this problem. Use this to avoid repeating guidance and to judge how much more specific the next hint should be.",
            ParametersJsonSchema = """{"type":"object","properties":{"studentId":{"type":"string","description":"The student's user ID"},"problemId":{"type":"string","description":"The problem ID"}},"required":["studentId","problemId"]}"""
        }
    ];

    public static async Task<string> ExecuteToolCallAsync(
        string toolName,
        JsonElement arguments,
        TutorAgentInput input,
        ITutorAgentTools tools)
    {
        var studentId = ReadGuid(arguments, "studentId") ?? input.UserId;
        var problemId = ReadGuid(arguments, "problemId") ?? input.ProblemId;

        return toolName switch
        {
            "get_attempt_history" => JsonSerializer.Serialize(await tools.GetAttemptHistoryAsync(studentId, problemId)),
            "search_knowledge_base" => await ExecuteSearchKnowledgeBaseAsync(arguments, tools),
            "check_partial_code" => await ExecuteCheckPartialCodeAsync(arguments, input, tools),
            "get_previous_hints" => JsonSerializer.Serialize(await tools.GetPreviousHintsAsync(studentId, problemId)),
            _ => """{"error":"Unknown tool"}"""
        };
    }

    private static async Task<string> ExecuteSearchKnowledgeBaseAsync(JsonElement args, ITutorAgentTools tools)
    {
        var query = args.TryGetProperty("query", out var q) ? q.GetString() ?? string.Empty : string.Empty;
        var conceptTag = args.TryGetProperty("conceptTag", out var t) ? t.GetString() : null;
        var results = await tools.SearchKnowledgeBaseAsync(query, conceptTag);
        return JsonSerializer.Serialize(results);
    }

    private static async Task<string> ExecuteCheckPartialCodeAsync(JsonElement args, TutorAgentInput input, ITutorAgentTools tools)
    {
        var code = args.TryGetProperty("code", out var c) ? c.GetString() ?? string.Empty : input.StudentCode ?? string.Empty;
        var language = args.TryGetProperty("language", out var l) ? l.GetString() ?? "Python" : "Python";
        var observation = await tools.CheckPartialCodeAsync(code, language);
        return JsonSerializer.Serialize(observation);
    }

    private static Guid? ReadGuid(JsonElement args, string property)
    {
        if (!args.TryGetProperty(property, out var element))
            return null;
        var raw = element.GetString();
        return Guid.TryParse(raw, out var parsed) ? parsed : null;
    }
}
