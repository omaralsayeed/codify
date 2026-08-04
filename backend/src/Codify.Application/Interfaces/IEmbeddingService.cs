namespace Codify.Application.Interfaces;

/// <summary>
/// Generates vector embeddings for text using a configured embedding model
/// (e.g. OpenAI text-embedding-3-small).
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate an embedding vector for a single piece of text.
    /// </summary>
    Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embedding vectors for multiple pieces of text in one call.
    /// </summary>
    Task<List<float[]>> GenerateBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
