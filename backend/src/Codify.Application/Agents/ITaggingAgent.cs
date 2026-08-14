namespace Codify.Application.Agents;

/// <summary>Input for classifying the concept tags of a problem.</summary>
public class TaggingAgentInput
{
    public string ProblemTitle { get; set; } = string.Empty;
    public string ProblemStatement { get; set; } = string.Empty;

    /// <summary>The allowed ConceptTag names. The agent may only choose from these.</summary>
    public List<string> AvailableTags { get; set; } = [];
}

/// <summary>Structured classification output from the Tagging Agent.</summary>
public class TagClassificationResult
{
    public List<string> AssignedTags { get; set; } = [];
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// The Tagging Agent classifies a problem's concept tags. It is a static
/// workflow (not agentic tool calling): retrieve RAG concept context, then a
/// single LLM classification call, then validation against the allowed tags.
/// </summary>
public interface ITaggingAgent
{
    Task<TagClassificationResult> ClassifyProblemTagsAsync(
        TaggingAgentInput input, CancellationToken cancellationToken = default);
}
