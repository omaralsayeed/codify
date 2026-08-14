using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.Search;

/// <summary>
/// High-level RAG retrieval over the Chroma Cloud knowledge base. Embeds the
/// query with text-embedding-3-small, runs a similarity search against the
/// concept documents stored in Chroma (source = "concept"), and formats the
/// top-k chunks for injection into agent prompts.
///
/// Fails gracefully: if embedding or retrieval throws (e.g. Chroma outage),
/// returns an empty list and logs — agents then proceed without RAG context.
/// </summary>
public class KnowledgeBaseSearchService(
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    ILogger<KnowledgeBaseSearchService> logger) : IKnowledgeBaseSearchService
{
    public async Task<List<KnowledgeBaseResult>> SearchAsync(
        string query,
        string? conceptTag = null,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        try
        {
            var vector = await embeddingService.GenerateAsync(query, cancellationToken);
            if (vector.Length == 0)
                return [];

            var results = await vectorStore.SearchAsync(
                vector,
                conceptTag: conceptTag,
                source: "concept",
                topK: topK,
                minSimilarity: null,
                cancellationToken: cancellationToken);

            return results.Select(r => new KnowledgeBaseResult
            {
                Content = r.Content,
                ConceptTag = r.Metadata.TryGetValue("concept_tag", out var tag)
                    ? tag?.ToString() ?? string.Empty
                    : string.Empty,
                Relevance = r.Similarity
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Knowledge base search failed for query '{Query}'. Returning empty results.", query);
            return [];
        }
    }
}
