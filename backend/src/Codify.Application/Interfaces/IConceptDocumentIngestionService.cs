namespace Codify.Application.Interfaces;

/// <summary>
/// Ingests concept documents and problem statements into the vector store
/// so that RAG agents can retrieve them.
/// </summary>
public interface IConceptDocumentIngestionService
{
    /// <summary>
    /// Ingest all concept markdown documents found in the configured directory.
    /// </summary>
    Task IngestConceptsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest a single problem statement into the vector store.
    /// </summary>
    Task IngestProblemAsync(Guid problemId, string title, string statement, List<string> conceptTags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest all existing problems from the SQL database.
    /// </summary>
    Task IngestAllProblemsAsync(CancellationToken cancellationToken = default);
}
