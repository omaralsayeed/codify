using Codify.Application.Agents;
using Codify.Application.DTOs.Execution;

namespace Codify.Application.Interfaces;

/// <summary>
/// Tools available to the .NET-native Code Analysis Agent. The agent decides
/// which tools to call via OpenAI function calling; these methods only execute
/// the requested operation and return structured data.
/// </summary>
public interface ICodeAnalysisAgentTools
{
    Task<ProblemToolResult> GetProblemAndTestCasesAsync(Guid problemId);
    Task<RunCodeResponse> RunSandboxedExecutionAsync(string code, string language, Guid problemId);
    Task<StaticAnalysisResult> RunStaticAnalysisAsync(string code, string language);
    Task<AttemptHistoryResult> GetSubmissionHistoryAsync(Guid userId, Guid problemId);
    Task<ComplexityEstimateResult> EstimateComplexityAsync(string code, string language, List<TestResultPayload> testResults);
}

public class ProblemToolResult
{
    public string Title { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public string Constraints { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int TimeLimitMs { get; set; }
    public int MemoryLimitMb { get; set; }
    public List<TestCasePayload> TestCases { get; set; } = [];
}

public class StaticAnalysisResult
{
    public List<StaticFindingPayload> Findings { get; set; } = [];
    public List<string> Observations { get; set; } = [];
}

public class ComplexityEstimateResult
{
    public string TimeComplexity { get; set; } = "Unknown";
    public string SpaceComplexity { get; set; } = "Unknown";
    public string Reasoning { get; set; } = string.Empty;
}
