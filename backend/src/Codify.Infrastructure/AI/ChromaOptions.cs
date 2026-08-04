namespace Codify.Infrastructure.AI;

/// <summary>
/// Options for the Chroma vector database connection.
/// Bound from the "Chroma" configuration section.
/// </summary>
public class ChromaOptions
{
    public const string SectionName = "Chroma";

    /// <summary>Base URL of the Chroma HTTP API (e.g. http://localhost:8000).</summary>
    public string BaseUrl { get; set; } = "http://localhost:8000";

    /// <summary>Name of the collection that stores concept and problem embeddings.</summary>
    public string CollectionName { get; set; } = "codify_knowledge";

    /// <summary>HTTP timeout in milliseconds for Chroma calls.</summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>Cosine-distance threshold under which results are considered relevant.</summary>
    public float SimilarityThreshold { get; set; } = 0.75f;
}
