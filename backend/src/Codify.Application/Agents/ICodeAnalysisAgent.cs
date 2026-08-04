using Codify.Domain.Enums;

namespace Codify.Application.Agents;

/// <summary>
/// Contract for the Code Analysis Agent. Implemented as a .NET-native agentic
/// service that uses OpenAI function calling to decide which tools to invoke.
/// The Application layer depends only on this interface, never on transport details.
/// </summary>
public interface ICodeAnalysisAgent
{
    Task<CodeAnalysisResult> AnalyzeAsync(CodeAnalysisAgentInput input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Input payload for the Code Analysis Agent. The service layer assembles this
/// from the problem and submission before delegating to the agent.
/// </summary>
public class CodeAnalysisAgentInput
{
    public Guid ProblemId { get; set; }
    public string Code { get; set; } = string.Empty;
    public SubmissionLanguage Language { get; set; }
    public string ProblemTitle { get; set; } = string.Empty;
    public string ProblemStatement { get; set; } = string.Empty;
    public string Constraints { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int TimeLimitMs { get; set; } = 2000;
    public int MemoryLimitMb { get; set; } = 256;
    public List<TestCasePayload> TestCases { get; set; } = [];
    public Guid? UserId { get; set; }
    public Guid? SubmissionId { get; set; }
}

public class TestCasePayload
{
    public string InputData { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public bool IsSample { get; set; }
    public int OrderIndex { get; set; }
}

/// <summary>
/// Structured result returned by the Code Analysis Agent. The service layer
/// maps this into FeedbackRecord entities.
/// </summary>
public class CodeAnalysisResult
{
    public string Verdict { get; set; } = string.Empty;
    public string CodeQualityFeedback { get; set; } = string.Empty;
    public string OptimizationSuggestion { get; set; } = string.Empty;
    public string TimeComplexity { get; set; } = string.Empty;
    public string SpaceComplexity { get; set; } = string.Empty;
    public bool IntegrityFlag { get; set; }
    public string? IntegrityNote { get; set; }
    public string OverallMessage { get; set; } = string.Empty;
    public List<TestResultPayload> TestResults { get; set; } = [];
    public List<StaticFindingPayload> StaticFindings { get; set; } = [];
    public List<string> ToolsUsed { get; set; } = [];
    public string ReasoningSummary { get; set; } = string.Empty;
}

public class TestResultPayload
{
    public string InputData { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public string ActualOutput { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Stderr { get; set; } = string.Empty;
    public double ExecutionTimeMs { get; set; }
}

public class StaticFindingPayload
{
    public string RuleId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int? Line { get; set; }
    public string Message { get; set; } = string.Empty;
}
