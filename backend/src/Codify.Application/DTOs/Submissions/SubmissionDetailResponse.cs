using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Submissions;

public class SubmissionDetailResponse
{
    public Guid SubmissionId { get; set; }
    public Guid ProblemId { get; set; }
    public Guid UserId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int? ExecutionTimeMs { get; set; }
    public int? MemoryUsedKb { get; set; }
    public int PassedTestCases { get; set; }
    public int TotalTestCases { get; set; }
    public decimal? Score { get; set; }
    public SubmissionResultDetail? Result { get; set; }
    public List<FeedbackDetail> AiFeedback { get; set; } = [];
}

public class SubmissionResultDetail
{
    public int PassedTestCount { get; set; }
    public int FailedTestCount { get; set; }
    public int TotalTestCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OutputSummary { get; set; }
}

public class FeedbackDetail
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
