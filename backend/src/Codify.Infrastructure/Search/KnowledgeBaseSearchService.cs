using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.Search;

/// <summary>
/// Searches the knowledge base (DSA concept docs) for the Tutor Agent using
/// Chroma vector similarity search. Queries are embedded and top-k relevant
/// concept chunks are returned.
/// </summary>
public class KnowledgeBaseSearchService(
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    ILogger<KnowledgeBaseSearchService> logger) : IKnowledgeBaseSearchService
{
    public async Task<List<KnowledgeBaseResult>> SearchAsync(
        string query, string? conceptTag = null, int topK = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        try
        {
            await vectorStore.EnsureCollectionAsync();
            var vector = await embeddingService.GenerateAsync(query);
            var results = await vectorStore.SearchAsync(
                vector,
                conceptTag: conceptTag,
                source: "concept",
                topK: topK,
                minSimilarity: null);

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
            logger.LogError(ex, "Vector knowledge base search failed for query '{Query}'. Falling back to empty results.", query);
            return [];
        }
    }
}
