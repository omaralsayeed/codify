namespace Codify.Application.DTOs.Execution;

/// <summary>
/// Everything Judge0 needs to run one piece of source code against one stdin input.
/// </summary>
public class Judge0SubmissionRequest
{
    public string SourceCode { get; set; } = string.Empty;
    public int LanguageId { get; set; }
    public string? Stdin { get; set; }

    /// <summary>CPU time limit, in seconds (Judge0's native unit).</summary>
    public double CpuTimeLimitSeconds { get; set; }

    /// <summary>Memory limit, in kilobytes (Judge0's native unit).</summary>
    public int MemoryLimitKb { get; set; }
}
