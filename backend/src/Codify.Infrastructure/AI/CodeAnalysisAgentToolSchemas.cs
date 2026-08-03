using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.Interfaces;

namespace Codify.Infrastructure.AI;

/// <summary>
/// OpenAI function-calling tool definitions for the .NET-native Code Analysis Agent.
/// These are sent to the model so it can decide which tools to call.
/// The model — not our C# code — decides which tools to invoke, in what
/// order, and how many.
/// </summary>
public static class CodeAnalysisAgentToolSchemas
{
    public static List<LlmToolDefinition> GetAll() =>
    [
        new LlmToolDefinition
        {
            Name = "get_problem_and_test_cases",
            Description = "Load problem metadata and test cases. Use this to understand the problem the student is solving before analyzing their code.",
            ParametersJsonSchema = """{"type":"object","properties":{"problemId":{"type":"string","description":"The problem ID"}},"required":["problemId"]}"""
        },
        new LlmToolDefinition
        {
            Name = "run_sandboxed_execution",
            Description = "Run the submitted code against sample test cases in an isolated sandbox. Use this to verify functional correctness and observe runtime behavior.",
            ParametersJsonSchema = """{"type":"object","properties":{"code":{"type":"string","description":"The code to execute"},"language":{"type":"string","description":"The programming language (Python or CSharp)"},"problemId":{"type":"string","description":"The problem ID"}},"required":["code","language","problemId"]}"""
        },
        new LlmToolDefinition
        {
            Name = "run_static_analysis",
            Description = "Get lightweight static observations about the code: structure, potential syntax issues, missing patterns, and common mistakes. NOT a full code review.",
            ParametersJsonSchema = """{"type":"object","properties":{"code":{"type":"string","description":"The code to analyze"},"language":{"type":"string","description":"The programming language (Python or CSharp)"}},"required":["code","language"]}"""
        },
        new LlmToolDefinition
        {
            Name = "get_submission_history",
            Description = "Get the student's prior submission statuses for this problem. Use this to understand if this is a first attempt or a repeated failure pattern.",
            ParametersJsonSchema = """{"type":"object","properties":{"userId":{"type":"string","description":"The student's user ID"},"problemId":{"type":"string","description":"The problem ID"}},"required":["userId","problemId"]}"""
        },
        new LlmToolDefinition
        {
            Name = "estimate_complexity",
            Description = "Estimate the time and space complexity of the code based on structure and execution results. Use this to provide accurate complexity information rather than guessing.",
            ParametersJsonSchema = """{"type":"object","properties":{"code":{"type":"string","description":"The code to analyze"},"language":{"type":"string","description":"The programming language (Python or CSharp)"}},"required":["code","language"]}"""
        }
    ];

    public static async Task<string> ExecuteToolCallAsync(
        string toolName,
        JsonElement arguments,
        CodeAnalysisAgentInput input,
        ICodeAnalysisAgentTools tools)
    {
        return toolName switch
        {
            "get_problem_and_test_cases" => await ExecuteGetProblemAndTestCasesAsync(arguments, input, tools),
            "run_sandboxed_execution" => await ExecuteRunSandboxedExecutionAsync(arguments, input, tools),
            "run_static_analysis" => await ExecuteRunStaticAnalysisAsync(arguments, input, tools),
            "get_submission_history" => await ExecuteGetSubmissionHistoryAsync(arguments, input, tools),
            "estimate_complexity" => await ExecuteEstimateComplexityAsync(arguments, input, tools),
            _ => """{"error":"Unknown tool"}"""
        };
    }

    private static async Task<string> ExecuteGetProblemAndTestCasesAsync(
        JsonElement args, CodeAnalysisAgentInput input, ICodeAnalysisAgentTools tools)
    {
        var problemId = args.TryGetProperty("problemId", out var p) ? Guid.Parse(p.GetString()!) : input.ProblemId;
        var result = await tools.GetProblemAndTestCasesAsync(problemId);
        return JsonSerializer.Serialize(result);
    }

    private static async Task<string> ExecuteRunSandboxedExecutionAsync(
        JsonElement args, CodeAnalysisAgentInput input, ICodeAnalysisAgentTools tools)
    {
        var code = args.TryGetProperty("code", out var c) ? c.GetString()! : input.Code;
        var language = args.TryGetProperty("language", out var l) ? l.GetString()! : input.Language.ToString();
        var problemId = args.TryGetProperty("problemId", out var p) ? Guid.Parse(p.GetString()!) : input.ProblemId;
        var result = await tools.RunSandboxedExecutionAsync(code, language, problemId);
        return JsonSerializer.Serialize(result);
    }

    private static async Task<string> ExecuteRunStaticAnalysisAsync(
        JsonElement args, CodeAnalysisAgentInput input, ICodeAnalysisAgentTools tools)
    {
        var code = args.TryGetProperty("code", out var c) ? c.GetString()! : input.Code;
        var language = args.TryGetProperty("language", out var l) ? l.GetString()! : input.Language.ToString();
        var result = await tools.RunStaticAnalysisAsync(code, language);
        return JsonSerializer.Serialize(result);
    }

    private static async Task<string> ExecuteGetSubmissionHistoryAsync(
        JsonElement args, CodeAnalysisAgentInput input, ICodeAnalysisAgentTools tools)
    {
        var userId = args.TryGetProperty("userId", out var u) ? Guid.Parse(u.GetString()!) : (input.UserId ?? Guid.Empty);
        var problemId = args.TryGetProperty("problemId", out var p) ? Guid.Parse(p.GetString()!) : input.ProblemId;
        var result = await tools.GetSubmissionHistoryAsync(userId, problemId);
        return JsonSerializer.Serialize(result);
    }

    private static async Task<string> ExecuteEstimateComplexityAsync(
        JsonElement args, CodeAnalysisAgentInput input, ICodeAnalysisAgentTools tools)
    {
        var code = args.TryGetProperty("code", out var c) ? c.GetString()! : input.Code;
        var language = args.TryGetProperty("language", out var l) ? l.GetString()! : input.Language.ToString();
        var result = await tools.EstimateComplexityAsync(code, language, []);
        return JsonSerializer.Serialize(result);
    }
}


