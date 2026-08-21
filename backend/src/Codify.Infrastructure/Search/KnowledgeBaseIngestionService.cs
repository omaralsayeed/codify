using System.Text;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.Search;

/// <summary>
/// Populates the Chroma Cloud knowledge base with concept-tag explanations and
/// problem statements. Each document is embedded via HuggingFace BAAI/bge-small-en-v1.5 
/// and stored with metadata (source type, concept tag, difficulty) so the Tutor Agent's
/// search_knowledge_base tool can retrieve grounded context at hint time.
/// </summary>
public class KnowledgeBaseIngestionService(
    IConceptTagRepository conceptTagRepo,
    IProblemRepository problemRepo,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    ILogger<KnowledgeBaseIngestionService> logger) : IKnowledgeBaseIngestionService
{
    private const int MaxChunkChars = 1600;
    private const int ChunkOverlapChars = 200;

    public async Task<int> IngestAllConceptsAsync(CancellationToken ct = default)
    {
        var tags = (await conceptTagRepo.GetAllAsync())
            .Where(t => !t.IsDeleted)
            .ToList();

        if (tags.Count == 0)
        {
            logger.LogInformation("No concept tags to ingest.");
            return 0;
        }

        await vectorStore.EnsureCollectionAsync(ct);

        var documents = tags.Select(tag => new VectorDocument
        {
            Id = $"concept-{tag.Id}",
            Content = $"{tag.Name}: {tag.Description}",
            Metadata = new Dictionary<string, object>
            {
                ["source"] = "concept",
                ["concept_tag"] = tag.Name,
                ["tag_id"] = tag.Id.ToString()
            }
        }).ToList();

        var texts = documents.Select(d => d.Content).ToList();
        var vectors = await embeddingService.GenerateBatchAsync(texts, ct);

        for (int i = 0; i < documents.Count && i < vectors.Count; i++)
            documents[i].Vector = vectors[i];

        var validDocs = documents.Where(d => d.Vector.Length > 0).ToList();
        if (validDocs.Count > 0)
            await vectorStore.UpsertAsync(validDocs, ct);

        logger.LogInformation("Ingested {Count} concept-tag documents into Chroma Cloud.", validDocs.Count);
        return validDocs.Count;
    }

    public async Task<int> IngestAllProblemsAsync(CancellationToken ct = default)
    {
        var problems = await problemRepo.GetAllActiveWithTagsAsync();

        if (problems.Count == 0)
        {
            logger.LogInformation("No active problems to ingest.");
            return 0;
        }

        await vectorStore.EnsureCollectionAsync(ct);

        var documents = new List<VectorDocument>();
        foreach (var problem in problems)
        {
            var firstTag = problem.ProblemTags
                .Select(pt => pt.ConceptTag?.Name)
                .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "General";

            var chunks = ChunkText(problem.Statement, MaxChunkChars, ChunkOverlapChars);

            for (int i = 0; i < chunks.Count; i++)
            {
                var content = $"Problem: {problem.Title}\nDifficulty: {problem.Difficulty}\n\n{chunks[i]}";
                var suffix = chunks.Count > 1 ? $"-chunk{i}" : string.Empty;

                documents.Add(new VectorDocument
                {
                    Id = $"problem-{problem.Id}{suffix}",
                    Content = content,
                    Metadata = new Dictionary<string, object>
                    {
                        ["source"] = "problem",
                        ["concept_tag"] = firstTag,
                        ["problem_id"] = problem.Id.ToString(),
                        ["difficulty"] = problem.Difficulty.ToString(),
                        ["title"] = problem.Title
                    }
                });
            }
        }

        if (documents.Count == 0)
        {
            logger.LogInformation("No problem documents produced.");
            return 0;
        }

        var texts = documents.Select(d => d.Content).ToList();
        var vectors = await embeddingService.GenerateBatchAsync(texts, ct);

        for (int i = 0; i < documents.Count && i < vectors.Count; i++)
            documents[i].Vector = vectors[i];

        var validDocs = documents.Where(d => d.Vector.Length > 0).ToList();
        if (validDocs.Count > 0)
            await vectorStore.UpsertAsync(validDocs, ct);

        logger.LogInformation("Ingested {Count} problem documents into Chroma Cloud.", validDocs.Count);
        return validDocs.Count;
    }

    public async Task<IngestionResult> ReindexAllAsync(CancellationToken ct = default)
    {
        var concepts = await IngestAllConceptsAsync(ct);
        var problems = await IngestAllProblemsAsync(ct);
        return new IngestionResult { ConceptsIngested = concepts, ProblemsIngested = problems };
    }

    private static List<string> ChunkText(string text, int maxChars, int overlapChars)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        text = text.Trim();
        if (text.Length <= maxChars) return [text];

        var chunks = new List<string>();
        var start = 0;

        while (start < text.Length)
        {
            var end = Math.Min(start + maxChars, text.Length);
            if (end < text.Length)
            {
                var lastNewline = text.LastIndexOf('\n', end - 1, end - start);
                if (lastNewline > start + maxChars / 2) end = lastNewline + 1;
                else
                {
                    var lastSpace = text.LastIndexOf(' ', end - 1, end - start);
                    if (lastSpace > start + maxChars / 2) end = lastSpace;
                }
            }

            chunks.Add(text[start..end].Trim());
            if (end >= text.Length) break;
            start = Math.Max(0, end - overlapChars);
        }

        return chunks.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
    }
}
