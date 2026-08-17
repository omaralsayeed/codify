namespace Codify.Application.DTOs.Admin;

/// <summary>
/// A single recent submission entry nested inside AdminUserDetailResponse.
/// </summary>
public class AdminUserSubmissionRow
{
    public string ProblemTitle { get; set; } = string.Empty;

    /// <summary>
    /// Submission status string matching the SubmissionStatus enum values:
    /// "Accepted", "WrongAnswer", "RuntimeError", "TimeLimitExceeded", etc.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; }
}
