using Codify.Domain.Enums;

namespace Codify.Domain.Entities;

public class Submission
{
    public Guid Id { get; private set; }
    public Guid ProblemId { get; private set; }
    public Guid UserId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public SubmissionLanguage Language { get; private set; }
    public SubmissionStatus Status { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public int? ExecutionTimeMs { get; private set; }
    public int? MemoryUsedKb { get; private set; }

    // Navigation
    public Problem Problem { get; private set; } = null!;
    public User User { get; private set; } = null!;
    public SubmissionResult? Result { get; private set; }
    public ICollection<FeedbackRecord> FeedbackRecords { get; private set; } = [];

    private Submission() { }

    public static Submission Create(Guid problemId, Guid userId, string code, SubmissionLanguage language)
    {
        return new Submission
        {
            Id = Guid.NewGuid(),
            ProblemId = problemId,
            UserId = userId,
            Code = code,
            Language = language,
            Status = SubmissionStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };
    }

    public void MarkAsRunning() => Status = SubmissionStatus.Running;
    public void MarkAsAccepted(int executionTimeMs, int memoryUsedKb)
    {
        Status = SubmissionStatus.Accepted;
        ExecutionTimeMs = executionTimeMs;
        MemoryUsedKb = memoryUsedKb;
    }
    public void MarkAsFailed(SubmissionStatus reason) => Status = reason;
}
