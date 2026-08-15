namespace Codify.Application.Interfaces;

/// <summary>A document stored in the vector database with its embedding and metadata.</summary>
public class VectorDocument
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float[] Vector { get; set; } = [];

    /// <summary>
    /// Filterable metadata. Expected keys used across Codify:
    /// "source" ("concept" | "problem") and "concept_tag".
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>A single hit returned by a vector similarity search.</summary>
public class VectorSearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    /// <summary>Normalized similarity in [0,1]; higher means more relevant.</summary>
    public float Similarity { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Abstraction over the vector database (Chroma Cloud in production). The
/// application depends only on this contract, so the underlying store can be
/// swapped or mocked in tests.
/// </summary>
public interface IVectorStore
{
    /// <summary>Ensures the configured collection exists (idempotent).</summary>
    Task EnsureCollectionAsync(CancellationToken cancellationToken = default);

    /// <summary>Inserts or overwrites documents (keyed by Id).</summary>
    Task UpsertAsync(IReadOnlyList<VectorDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a nearest-neighbour search for the given query vector. Optional
    /// metadata filters narrow the candidates. Returns results ordered most
    /// relevant first. Implementations must fail gracefully (return empty).
    /// </summary>
    Task<List<VectorSearchResult>> SearchAsync(
        float[] queryVector,
        string? conceptTag = null,
        string? source = null,
        int topK = 5,
        float? minSimilarity = null,
        CancellationToken cancellationToken = default);
}
