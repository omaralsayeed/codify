using Codify.Domain.Enums;

namespace Codify.Application.Agents;

public class TutorAgentInput
{
    public Guid ProblemId { get; set; }
    public string ProblemTitle { get; set; } = string.Empty;
    public string ProblemStatement { get; set; } = string.Empty;
    public List<string> ConceptTags { get; set; } = [];
    public int HintLevel { get; set; }
    public List<string> PreviousHints { get; set; } = [];
    public SubmissionStatus? LastSubmissionStatus { get; set; }
    public int AttemptCount { get; set; }
    public string RetrievedContext { get; set; } = string.Empty;
    public string? StudentCode { get; set; }
}
