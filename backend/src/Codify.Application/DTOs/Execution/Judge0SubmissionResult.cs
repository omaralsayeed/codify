namespace Codify.Application.DTOs.Execution;

/// <summary>
/// The terminal (or timed-out) state of a Judge0 submission, as reported by
/// GET /submissions/{token}.
/// </summary>
public class Judge0SubmissionResult
{
    /// <summary>Judge0's numeric status id — see <see cref="Codify.Application.Execution.Judge0Status"/>.</summary>
    public int StatusId { get; set; }
    public string StatusDescription { get; set; } = string.Empty;

    public string? Stdout { get; set; }
    public string? Stderr { get; set; }
    public string? CompileOutput { get; set; }
    public string? Message { get; set; }

    /// <summary>Judge0 reports wall time as a decimal-seconds string, e.g. "0.012".</summary>
    public string? TimeSeconds { get; set; }
    public int? MemoryKb { get; set; }

    /// <summary>
    /// True when our polling budget was exhausted before Judge0 reached a terminal status
    /// (still "In Queue" or "Processing"). Treated as a timeout by the caller.
    /// </summary>
    public bool PollTimedOut { get; set; }
}
