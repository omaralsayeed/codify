namespace Codify.Application.Interfaces;

/// <summary>
/// Generates dense vector embeddings for text. Used to embed both queries and
/// knowledge-base documents so they can be stored/compared in Chroma Cloud.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>Embeds a single piece of text and returns its vector.</summary>
    Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Embeds a batch of texts. Empty/blank inputs are skipped.</summary>
    Task<List<float[]>> GenerateBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
