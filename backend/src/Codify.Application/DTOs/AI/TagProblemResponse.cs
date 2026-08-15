namespace Codify.Application.DTOs.AI;

/// <summary>Result of tagging a single problem via the Tagging Agent.</summary>
public class TagProblemResponse
{
    public Guid ProblemId { get; set; }
    public List<string> AssignedTags { get; set; } = [];
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>True when the problem already had tags and was left unchanged.</summary>
    public bool AlreadyTagged { get; set; }
}

/// <summary>Result of the automatic scan that tags all untagged problems.</summary>
public class TagScanResponse
{
    public int UntaggedFound { get; set; }
    public int Tagged { get; set; }
    public List<TagProblemResponse> Results { get; set; } = [];
}
