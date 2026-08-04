using Codify.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.Search;

/// <summary>
/// Reads concept markdown documents and problem statements, embeds them, and
/// upserts them into the Chroma vector store for RAG retrieval.
/// </summary>
public class ConceptDocumentIngestionService(
    IVectorStore vectorStore,
    IEmbeddingService embeddingService,
    IProblemRepository problemRepo,
    IConfiguration configuration,
    ILogger<ConceptDocumentIngestionService> logger) : IConceptDocumentIngestionService
{
    public async Task IngestConceptsAsync(CancellationToken cancellationToken = default)
    {
        await vectorStore.EnsureCollectionAsync(cancellationToken);

        var conceptsPath = configuration["Concepts:Path"] ?? "data/concepts";
        var root = Path.Combine(AppContext.BaseDirectory, conceptsPath);
        if (!Directory.Exists(root))
        {
            logger.LogWarning("Concept documents directory not found: {Path}", root);
            return;
        }

        var files = Directory.GetFiles(root, "*.md", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            logger.LogWarning("No concept markdown files found in {Path}", root);
            return;
        }

        var documents = new List<VectorDocument>();
        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file, cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
                continue;

            var conceptTag = InferConceptTag(file, content);
            var chunks = ChunkDocument(content, maxChars: 1200);
            var embeddings = await embeddingService.GenerateBatchAsync(chunks, cancellationToken);

            for (int i = 0; i < chunks.Count; i++)
            {
                documents.Add(new VectorDocument
                {
                    Id = $"concept:{conceptTag}:{i}",
                    Content = chunks[i],
                    Vector = embeddings[i],
                    Metadata = new Dictionary<string, object>
                    {
                        ["source"] = "concept",
                        ["concept_tag"] = conceptTag,
                        ["file"] = Path.GetFileName(file),
                        ["chunk_index"] = i
                    }
                });
            }
        }

        await vectorStore.UpsertAsync(documents, cancellationToken);
        logger.LogInformation("Ingested {Count} concept chunks from {Files} files.", documents.Count, files.Length);
    }


    public async Task IngestProblemAsync(Guid problemId, string title, string statement, List<string> conceptTags, CancellationToken cancellationToken = default)
    {
        await vectorStore.EnsureCollectionAsync(cancellationToken);

        var text = $"Problem: {title}\n\n{statement}";
        var vector = await embeddingService.GenerateAsync(text, cancellationToken);

        await vectorStore.UpsertAsync([
            new VectorDocument
            {
                Id = $"problem:{problemId}",
                Content = text,
                Vector = vector,
                Metadata = new Dictionary<string, object>
                {
                    ["source"] = "problem",
                    ["problem_id"] = problemId.ToString(),
                    ["concept_tag"] = conceptTags.FirstOrDefault() ?? "General",
                    ["title"] = title
                }
            }
        ], cancellationToken);
    }

    public async Task IngestAllProblemsAsync(CancellationToken cancellationToken = default)
    {
        await vectorStore.EnsureCollectionAsync(cancellationToken);

        var problems = await problemRepo.GetAllAsync();
        var documents = new List<VectorDocument>();

        foreach (var problem in problems)
        {
            var text = $"Problem: {problem.Title}\n\n{problem.Statement}";
            var vector = await embeddingService.GenerateAsync(text, cancellationToken);
            var tags = problem.ProblemTags.Select(pt => pt.ConceptTag.Name).ToList();

            documents.Add(new VectorDocument
            {
                Id = $"problem:{problem.Id}",
                Content = text,
                Vector = vector,
                Metadata = new Dictionary<string, object>
                {
                    ["source"] = "problem",
                    ["problem_id"] = problem.Id.ToString(),
                    ["concept_tag"] = tags.FirstOrDefault() ?? "General",
                    ["title"] = problem.Title
                }
            });
        }

        await vectorStore.UpsertAsync(documents, cancellationToken);
        logger.LogInformation("Ingested {Count} problems into vector store.", documents.Count);
    }

    private static string InferConceptTag(string filePath, string content)
    {
        var firstLine = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
        if (!string.IsNullOrEmpty(firstLine) && firstLine.StartsWith('#'))
        {
            var tag = firstLine.TrimStart('#').Trim();
            if (!string.IsNullOrWhiteSpace(tag))
                return tag;
        }

        return Path.GetFileNameWithoutExtension(filePath)
            .Replace('-', ' ')
            .Replace('_', ' ');
    }

    private static List<string> ChunkDocument(string content, int maxChars)
    {
        var chunks = new List<string>();
        var paragraphs = content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var current = new System.Text.StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (current.Length + paragraph.Length > maxChars && current.Length > 0)
            {
                chunks.Add(current.ToString().Trim());
                current.Clear();
            }
            current.AppendLine(paragraph);
            current.AppendLine();
        }

        if (current.Length > 0)
            chunks.Add(current.ToString().Trim());

        if (chunks.Count == 0)
            chunks.Add(content.Trim());

        return chunks;
    }
}
