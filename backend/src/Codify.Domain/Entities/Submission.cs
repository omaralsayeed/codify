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

    // ER diagram additions
    public int PassedTestCases { get; private set; }
    public int TotalTestCases { get; private set; }
    public decimal? Score { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

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
            SubmittedAt = DateTime.UtcNow,
            PassedTestCases = 0,
            TotalTestCases = 0,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void MarkAsRunning()
    {
        Status = SubmissionStatus.Running;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsAccepted(int executionTimeMs, int memoryUsedKb, int passedTestCases, int totalTestCases)
    {
        Status = SubmissionStatus.Accepted;
        ExecutionTimeMs = executionTimeMs;
        MemoryUsedKb = memoryUsedKb;
        PassedTestCases = passedTestCases;
        TotalTestCases = totalTestCases;
        Score = totalTestCases > 0 ? (decimal)passedTestCases / totalTestCases * 100 : 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(SubmissionStatus reason, int passedTestCases, int totalTestCases)
    {
        Status = reason;
        PassedTestCases = passedTestCases;
        TotalTestCases = totalTestCases;
        Score = totalTestCases > 0 ? (decimal)passedTestCases / totalTestCases * 100 : 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
