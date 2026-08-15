namespace Codify.Application.Interfaces;

/// <summary>One retrieved knowledge-base chunk, formatted for agent context.</summary>
public class KnowledgeBaseResult
{
    public string Content { get; set; } = string.Empty;
    public string ConceptTag { get; set; } = string.Empty;

    /// <summary>Relevance/similarity score in [0,1].</summary>
    public float Relevance { get; set; }
}

/// <summary>
/// High-level retrieval over the Chroma Cloud knowledge base. Embeds a natural
/// language query and returns the most relevant concept chunks. This is the
/// RAG retrieval entry point used by the agents at execution time — all source
/// documents live in Chroma Cloud (pre-populated), not in local files.
/// </summary>
public interface IKnowledgeBaseSearchService
{
    Task<List<KnowledgeBaseResult>> SearchAsync(
        string query,
        string? conceptTag = null,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
