using System.Text.RegularExpressions;
using Codify.Application.Agents;
using Codify.Application.DTOs.Execution;
using Codify.Application.Interfaces;
using Codify.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.AI;

/// <summary>
/// Implements the tools the .NET-native Code Analysis Agent can call via
/// OpenAI function calling. The LLM decides which tools to call; these methods
/// only execute the requested tool and return structured data.
/// </summary>
public class CodeAnalysisAgentTools(
    IProblemRepository problemRepo,
    ISubmissionRepository submissionRepo,
    IExecutionService executionService,
    ILogger<CodeAnalysisAgentTools> logger) : ICodeAnalysisAgentTools
{
    public async Task<ProblemToolResult> GetProblemAndTestCasesAsync(Guid problemId)
    {
        var problem = await problemRepo.GetByIdWithTestCasesAsync(problemId);
        if (problem is null)
            return new ProblemToolResult { Title = "Not found" };

        return new ProblemToolResult
        {
            Title = problem.Title,
            Statement = problem.Statement,
            Constraints = problem.Constraints,
            Difficulty = problem.Difficulty.ToString(),
            TimeLimitMs = problem.TimeLimitMs,
            MemoryLimitMb = problem.MemoryLimitMb,
            TestCases = problem.TestCases
                .OrderBy(tc => tc.OrderIndex)
                .Select(tc => new TestCasePayload
                {
                    InputData = tc.InputData,
                    ExpectedOutput = tc.ExpectedOutput,
                    IsSample = tc.IsSample,
                    OrderIndex = tc.OrderIndex
                }).ToList()
        };
    }

    public async Task<RunCodeResponse> RunSandboxedExecutionAsync(string code, string language, Guid problemId)
    {
        try
        {
            if (!Enum.TryParse<SubmissionLanguage>(language, true, out var lang))
                lang = SubmissionLanguage.Python;

            var request = new RunCodeRequest
            {
                ProblemId = problemId,
                Code = code,
                Language = lang
            };

            return await executionService.RunAsync(request);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Sandboxed execution failed in code analysis tool.");
            return new RunCodeResponse
            {
                Status = "ExecutionUnavailable",
                Stderr = $"Execution service unavailable: {ex.Message}",
                TestResults = []
            };
        }
    }

    public Task<StaticAnalysisResult> RunStaticAnalysisAsync(string code, string language)
    {
        var findings = new List<StaticFindingPayload>();
        var observations = new List<string>();
        var codeNormalized = code ?? string.Empty;
        var lang = (language ?? string.Empty).ToLowerInvariant();
        var codeLower = codeNormalized.ToLowerInvariant();

        var hasFor = codeLower.Contains("for ") || codeLower.Contains("for(");
        var hasWhile = codeLower.Contains("while ") || codeLower.Contains("while(");
        if (!hasFor && !hasWhile)
            observations.Add("No explicit loop detected.");

        var hasRecursion = DetectRecursion(codeNormalized, lang);
        if (hasRecursion)
        {
            observations.Add("Recursive call present.");
            var hasBaseCase = Regex.IsMatch(codeLower, @"\bif\b.*\breturn\b");
            if (!hasBaseCase)
                findings.Add(new StaticFindingPayload
                {
                    RuleId = "RECURSION_NO_BASE",
                    Severity = "Warning",
                    Message = "Recursive call detected but no obvious base case found."
                });
        }

        var hasHashMap = codeLower.Contains("dict") || codeLower.Contains("dictionary")
            || codeLower.Contains("hashmap") || codeLower.Contains("hash map");
        if (!hasHashMap)
            observations.Add("No hash map / dictionary usage detected.");

        if (lang.Contains("py") || lang.Contains("python"))
        {
            if (!codeNormalized.Contains(':'))
                findings.Add(new StaticFindingPayload
                {
                    RuleId = "PY_MISSING_COLON",
                    Severity = "Error",
                    Message = "No colons found - possible Python syntax issue."
                });

            var inputCount = Regex.Matches(codeNormalized, @"input\s*\(").Count;
            if (inputCount == 0)
                observations.Add("No input() calls detected - ensure the program reads stdin if required.");
        }
        else if (lang.Contains("cs") || lang.Contains("csharp"))
        {
            if (!codeLower.Contains("console.readline") && !codeLower.Contains("parse"))
                observations.Add("No obvious input parsing detected - ensure the program reads input if required.");

            if (codeNormalized.Contains("class Program") && !codeNormalized.Contains("static void Main"))
                findings.Add(new StaticFindingPayload
                {
                    RuleId = "CS_NO_MAIN",
                    Severity = "Error",
                    Message = "C# program appears to be missing a Main entry point."
                });
        }

        return Task.FromResult(new StaticAnalysisResult
        {
            Findings = findings,
            Observations = observations
        });
    }



    public async Task<AttemptHistoryResult> GetSubmissionHistoryAsync(Guid userId, Guid problemId)
    {
        var submissions = await submissionRepo.GetByProblemAndUserAsync(problemId, userId);
        var subList = submissions.ToList();

        return new AttemptHistoryResult
        {
            AttemptCount = subList.Count,
            SubmissionStatuses = subList.Select(s => s.Status.ToString()).ToList(),
            PreviousHintLevels = [],
            Timestamps = subList.Select(s => s.SubmittedAt.ToString("O")).ToList()
        };
    }

    public Task<ComplexityEstimateResult> EstimateComplexityAsync(
        string code, string language, List<TestResultPayload> testResults)
    {
        var lang = (language ?? string.Empty).ToLowerInvariant();
        var codeNormalized = code ?? string.Empty;
        var codeLower = codeNormalized.ToLowerInvariant();

        string timeComplexity = "Unknown";
        string reasoning = "Heuristic estimate based on code structure.";

        var nestedLoopDepth = EstimateLoopDepth(codeNormalized);
        var hasRecursion = DetectRecursion(codeNormalized, lang);

        if (hasRecursion)
        {
            timeComplexity = "O(2^n) or worse";
            reasoning = "Recursion detected; complexity depends on branching and memoization.";
        }
        else if (nestedLoopDepth >= 3)
        {
            timeComplexity = "O(n^3)";
        }
        else if (nestedLoopDepth == 2)
        {
            timeComplexity = "O(n^2)";
        }
        else if (nestedLoopDepth == 1)
        {
            timeComplexity = "O(n)";
        }
        else if (codeLower.Contains("binary search") || codeLower.Contains("log"))
        {
            timeComplexity = "O(log n)";
        }
        else
        {
            timeComplexity = "O(1)";
        }

        if (testResults.Count > 0 && testResults.Any(t => t.ExecutionTimeMs > 0))
        {
            var avgTime = testResults.Average(t => t.ExecutionTimeMs);
            reasoning += $" Average sample execution time: {avgTime:F1}ms.";
        }

        string spaceComplexity = "O(1)";
        if (codeLower.Contains("dict") || codeLower.Contains("dictionary")
            || codeLower.Contains("hashset") || codeLower.Contains("hash set"))
        {
            spaceComplexity = "O(n)";
            reasoning += " Uses hash-based storage.";
        }

        return Task.FromResult(new ComplexityEstimateResult
        {
            TimeComplexity = timeComplexity,
            SpaceComplexity = spaceComplexity,
            Reasoning = reasoning
        });
    }

    private static int EstimateLoopDepth(string code)
    {
        var lines = code.Split('\n');
        var maxDepth = 0;
        var currentDepth = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (Regex.IsMatch(trimmed, @"\b(for|while)\b"))
                currentDepth++;
            else if (trimmed.StartsWith("}") || trimmed.StartsWith("return"))
                currentDepth = Math.Max(0, currentDepth - 1);

            if (currentDepth > maxDepth)
                maxDepth = currentDepth;
        }

        return maxDepth;
    }

    private static bool DetectRecursion(string code, string lang)
    {
        var lowerLang = lang.ToLowerInvariant();
        if (lowerLang.Contains("py") || lowerLang.Contains("python"))
            return DetectPythonRecursion(code);

        return DetectCStyleRecursion(code);
    }

    private static bool DetectPythonRecursion(string code)
    {
        var lines = code.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("def ") && trimmed.Contains('('))
            {
                var name = trimmed[4..].Split('(')[0].Trim();
                if (name.Length > 0 && code.Contains(name + "("))
                    return true;
            }
        }
        return false;
    }

    private static bool DetectCStyleRecursion(string code)
    {
        var lines = code.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Contains('(') && trimmed.Contains(')'))
            {
                var beforeParen = trimmed.Split('(')[0].Trim();
                var parts = beforeParen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var name = parts[^1];
                    if (name.Length > 0 && code.Contains(name + "("))
                        return true;
                }
            }
        }
        return false;
    }
}
