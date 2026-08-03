using System.ComponentModel.DataAnnotations;
using Codify.Domain.Enums;

namespace Codify.Application.DTOs.AI;

/// <summary>
/// Request to analyze submitted code with the Code Analysis Agent.
/// </summary>
public class CodeAnalysisRequest
{
    [Required]
    public Guid ProblemId { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public SubmissionLanguage Language { get; set; }

    /// <summary>
    /// Optional: analyze an existing submission by id instead of ad-hoc code.
    /// When provided, the service loads the submission from the repository.
    /// </summary>
    public Guid? SubmissionId { get; set; }
}

/// <summary>
/// Structured analysis result returned to the client.
/// </summary>
public class CodeAnalysisResponse
{
    public string Verdict { get; set; } = string.Empty;
    public string CodeQualityFeedback { get; set; } = string.Empty;
    public string OptimizationSuggestion { get; set; } = string.Empty;
    public string TimeComplexity { get; set; } = string.Empty;
    public string SpaceComplexity { get; set; } = string.Empty;
    public bool IntegrityFlag { get; set; }
    public string? IntegrityNote { get; set; }
    public string OverallMessage { get; set; } = string.Empty;
    public List<AnalysisTestResult> TestResults { get; set; } = [];
    public List<AnalysisStaticFinding> StaticFindings { get; set; } = [];
    public List<string> ToolsUsed { get; set; } = [];
    public string ReasoningSummary { get; set; } = string.Empty;
}

public class AnalysisTestResult
{
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public string ActualOutput { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Stderr { get; set; } = string.Empty;
    public double ExecutionTimeMs { get; set; }
}

public class AnalysisStaticFinding
{
    public string RuleId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int? Line { get; set; }
    public string Message { get; set; } = string.Empty;
}
