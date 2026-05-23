namespace Codify.Domain.Entities;

public class SubmissionResult
{
    public Guid Id { get; private set; }
    public Guid SubmissionId { get; private set; }
    public int PassedTestCount { get; private set; }
    public int FailedTestCount { get; private set; }
    public int TotalTestCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? OutputSummary { get; private set; }

    // Navigation
    public Submission Submission { get; private set; } = null!;

    private SubmissionResult() { }

    public static SubmissionResult Create(
        Guid submissionId,
        int passed,
        int failed,
        int total,
        string? errorMessage = null,
        string? outputSummary = null)
    {
        return new SubmissionResult
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            PassedTestCount = passed,
            FailedTestCount = failed,
            TotalTestCount = total,
            ErrorMessage = errorMessage,
            OutputSummary = outputSummary
        };
    }
}
