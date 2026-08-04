namespace Codify.Application.Interfaces;

/// <summary>
/// Abstraction over a vector database (Chroma) that stores and searches
/// embedded concept documents and problem statements.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Ensure the target collection exists in Chroma.
    /// </summary>
    Task EnsureCollectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert embeddings into the vector store.
    /// </summary>
    Task UpsertAsync(List<VectorDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query the vector store with an embedding vector. Returns top-k nearest
    /// neighbours filtered by optional metadata and relevance threshold.
    /// </summary>
    Task<List<VectorSearchResult>> SearchAsync(
        float[] queryVector,
        string? conceptTag = null,
        string? source = null,
        int topK = 5,
        float? minSimilarity = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete documents that match the given metadata filter.
    /// </summary>
    Task DeleteWhereAsync(Dictionary<string, object> filter, CancellationToken cancellationToken = default);
}

/// <summary>
/// A document to be stored in the vector database.
/// </summary>
public class VectorDocument
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float[] Vector { get; set; } = [];
    public Dictionary<string, object> Metadata { get; set; } = [];
}

/// <summary>
/// A single result from a vector similarity search.
/// </summary>
public class VectorSearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float Similarity { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = [];
}
