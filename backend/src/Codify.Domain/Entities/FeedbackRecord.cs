using Codify.Domain.Enums;

namespace Codify.Domain.Entities;

public class FeedbackRecord
{
    public Guid Id { get; private set; }
    public Guid SubmissionId { get; private set; }
    public FeedbackType FeedbackType { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Submission Submission { get; private set; } = null!;

    private FeedbackRecord() { }

    public static FeedbackRecord Create(Guid submissionId, FeedbackType feedbackType, string message)
    {
        return new FeedbackRecord
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            FeedbackType = feedbackType,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };
    }
}
