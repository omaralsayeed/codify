namespace Codify.Application.DTOs.Analytics;

/// <summary>
/// Represents an AI-generated code flag for instructor review.
/// </summary>
public class IntegrityFlagResponse
{
    public Guid FeedbackId { get; set; }
    public Guid SubmissionId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string ProblemTitle { get; set; } = string.Empty;
    public Guid ProblemId { get; set; }
    public double Confidence { get; set; }
    public string Indicators { get; set; } = string.Empty;
    public DateTime FlaggedAt { get; set; }
}