using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Submissions;

public class SubmissionSummaryResponse
{
    public Guid SubmissionId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int? ExecutionTimeMs { get; set; }
    public int? MemoryUsedKb { get; set; }
}
