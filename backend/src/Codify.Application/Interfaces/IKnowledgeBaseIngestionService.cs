namespace Codify.Application.Interfaces;

/// <summary>
/// Populates the Chroma Cloud knowledge base with concept-tag explanations and
/// problem statements so the Tutor Agent's search_knowledge_base tool can return
/// real RAG-grounded context.
/// </summary>
public interface IKnowledgeBaseIngestionService
{
    /// <summary>
    /// Embeds and upserts all concept-tag descriptions into Chroma Cloud.
    /// Returns the number of documents ingested.
    /// </summary>
    Task<int> IngestAllConceptsAsync(CancellationToken ct = default);

    /// <summary>
    /// Embeds and upserts all active problem statements (chunked) into Chroma Cloud.
    /// Returns the number of documents ingested.
    /// </summary>
    Task<int> IngestAllProblemsAsync(CancellationToken ct = default);

    /// <summary>
    /// Reindexes both concepts and problems. Returns total documents ingested.
    /// </summary>
    Task<IngestionResult> ReindexAllAsync(CancellationToken ct = default);
}

/// <summary>Result of a full reindex operation.</summary>
public class IngestionResult
{
    public int ConceptsIngested { get; set; }
    public int ProblemsIngested { get; set; }
    public int TotalIngested => ConceptsIngested + ProblemsIngested;
}
