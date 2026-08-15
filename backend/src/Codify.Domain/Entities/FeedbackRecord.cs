using Codify.Domain.Enums;

namespace Codify.Domain.Entities;

public class FeedbackRecord
{
    public Guid Id { get; private set; }
    public Guid SubmissionId { get; private set; }
    public FeedbackType FeedbackType { get; private set; }
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Confidence score (0.0 to 1.0) for AI-generated feedback.
    /// Null for non-AI feedback or when confidence is not applicable.
    /// </summary>
    public double? Confidence { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Submission Submission { get; private set; } = null!;

    private FeedbackRecord() { }

    public static FeedbackRecord Create(Guid submissionId, FeedbackType feedbackType, string message, double? confidence = null)
    {
        return new FeedbackRecord
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            FeedbackType = feedbackType,
            Message = message,
            Confidence = confidence,
            CreatedAt = DateTime.UtcNow
        };
    }
}
