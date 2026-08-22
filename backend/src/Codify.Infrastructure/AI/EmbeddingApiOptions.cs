namespace Codify.Infrastructure.AI;

/// <summary>
/// Configuration for the HuggingFace Embedding API.
/// Uses the HuggingFace Inference API with feature-extraction pipeline.
/// </summary>
public class EmbeddingApiOptions
{
    public const string SectionName = "EmbeddingApi";

    /// <summary>
    /// Full URL to the HuggingFace feature-extraction endpoint including model path.
    /// Example: https://router.huggingface.co/hf-inference/models/BAAI/bge-small-en-v1.5/pipeline/feature-extraction
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// HuggingFace API key in the format "Bearer hf_xxxxxxxxxxxxxxxxxxxx".
    /// Should include the "Bearer " prefix.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout for embedding API calls in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
