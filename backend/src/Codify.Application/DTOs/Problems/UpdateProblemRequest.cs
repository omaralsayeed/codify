using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Problems;

/// <summary>
/// Partial update request for PATCH /api/problems/:id.
/// All fields are optional — only send what changed.
/// </summary>
public class UpdateProblemRequest
{
    public string? Title { get; set; }
    public string? Statement { get; set; }
    public Difficulty? Difficulty { get; set; }
    public string? Constraints { get; set; }
    public List<string>? LanguageSupport { get; set; }
    public List<Guid>? TagIds { get; set; }

    /// <summary>When provided, replaces the problem's active/inactive state.</summary>
    public bool? IsActive { get; set; }

    /// <summary>When provided, updates the execution time limit in milliseconds.</summary>
    public int? TimeLimitMs { get; set; }

    /// <summary>When provided, updates the memory limit in megabytes.</summary>
    public int? MemoryLimitMb { get; set; }

    /// <summary>
    /// When provided, replaces the problem's sample test cases entirely.
    /// Each entry must have non-empty Input and ExpectedOutput.
    /// </summary>
    public List<SampleTestCaseInput>? SampleTestCases { get; set; }
}

/// <summary>A sample test case as sent in create/update requests.</summary>
public class SampleTestCaseInput
{
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
}
