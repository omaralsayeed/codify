namespace Codify.Application.DTOs.Execution;

/// <summary>
/// The result of running one test case.
/// </summary>
public class TestCaseResult
{
    /// <summary>The input we fed into the program.</summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>What we expected the program to print.</summary>
    public string Expected { get; set; } = string.Empty;

    /// <summary>What the program actually printed.</summary>
    public string Actual { get; set; } = string.Empty;

    /// <summary>True if actual matches expected exactly.</summary>
    public bool Passed { get; set; }

    /// <summary>
    /// "success", "error", or "timeout" — only relevant if Passed is false.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Error message if the code crashed on this test case.</summary>
    public string Stderr { get; set; } = string.Empty;

    /// <summary>How long this specific test case took in milliseconds.</summary>
    public int ExecutionTimeMs { get; set; }
}
