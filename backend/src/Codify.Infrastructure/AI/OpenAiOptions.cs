namespace Codify.Infrastructure.AI;

public class OpenAiOptions
{
    public const string SectionName = "OpenAI";
    public const string DefaultModel = "gpt-4o-mini";
    public const string DefaultEscalationModel = "gpt-4o";
    public const string DefaultEmbeddingModel = "text-embedding-3-small";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = DefaultModel;

    /// <summary>
    /// Model used when the Tutor Agent detects a complex case (high attempt count +
    /// high hint level). More expensive but better at multi-step reasoning.
    /// </summary>
    public string EscalationModel { get; set; } = DefaultEscalationModel;

    /// <summary>Hint level at or above which escalation is considered.</summary>
    public int EscalationHintLevelThreshold { get; set; } = 2;

    /// <summary>Attempt count at or above which escalation is considered.</summary>
    public int EscalationAttemptThreshold { get; set; } = 3;

    /// <summary>Model used to generate embeddings for the Chroma Cloud RAG layer.</summary>
    public string EmbeddingModel { get; set; } = DefaultEmbeddingModel;

    /// <summary>
    /// Custom base URL for OpenAI API calls. If empty, uses the official OpenAI API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}
