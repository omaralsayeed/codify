using System.Net.Http.Json;
using System.Text.Json;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Codify.Infrastructure.AI;

/// <summary>
/// Chroma vector database client. Talks to the Chroma HTTP API and stores
/// concept documents and problem statements for RAG.
/// </summary>
public class ChromaVectorStore(
    HttpClient httpClient,
    IOptions<ChromaOptions> options,
    ILogger<ChromaVectorStore> logger) : IVectorStore
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ChromaOptions _options = options.Value;
    private readonly ILogger<ChromaVectorStore> _logger = logger;

    public async Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                name = _options.CollectionName,
                metadata = new { description = "Codify knowledge base" }
            };
            var response = await _httpClient.PostAsJsonAsync("/api/v1/collections", payload, cancellationToken);
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Conflict)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Chroma collection creation returned {Status}: {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to ensure Chroma collection {Collection}.", _options.CollectionName);
        }
    }

    public async Task UpsertAsync(List<VectorDocument> documents, CancellationToken cancellationToken = default)
    {
        if (documents.Count == 0) return;

        var ids = documents.Select(d => d.Id).ToList();
        var embeddings = documents.Select(d => d.Vector).ToList();
        var metadatas = documents.Select(d => d.Metadata).ToList();
        var contents = documents.Select(d => d.Content).ToList();

        var payload = new
        {
            ids,
            embeddings,
            metadatas,
            documents = contents
        };

        var url = $"/api/v1/collections/{_options.CollectionName}/upsert";
        var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }


    public async Task<List<VectorSearchResult>> SearchAsync(
        float[] queryVector,
        string? conceptTag = null,
        string? source = null,
        int topK = 5,
        float? minSimilarity = null,
        CancellationToken cancellationToken = default)
    {
        var where = BuildWhereClause(conceptTag, source);
        var payload = new
        {
            query_embeddings = new[] { queryVector },
            n_results = topK,
            where,
            include = new[] { "metadatas", "documents", "distances" }
        };

        var url = $"/api/v1/collections/{_options.CollectionName}/query";
        var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Chroma query failed: {Status} {Error}", response.StatusCode, error);
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseQueryResponse(json, minSimilarity ?? _options.SimilarityThreshold);
    }

    public async Task DeleteWhereAsync(Dictionary<string, object> filter, CancellationToken cancellationToken = default)
    {
        var url = $"/api/v1/collections/{_options.CollectionName}/delete";
        var response = await _httpClient.PostAsJsonAsync(url, new { where = filter }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static Dictionary<string, object>? BuildWhereClause(string? conceptTag, string? source)
    {
        var clauses = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(conceptTag))
            clauses["concept_tag"] = conceptTag;
        if (!string.IsNullOrWhiteSpace(source))
            clauses["source"] = source;
        return clauses.Count > 0 ? clauses : null;
    }

    private static List<VectorSearchResult> ParseQueryResponse(string json, float minSimilarity)
    {
        using var doc = JsonDocument.Parse(json);
        var results = new List<VectorSearchResult>();

        if (!doc.RootElement.TryGetProperty("ids", out var idsElement)
            || idsElement.GetArrayLength() == 0)
            return results;

        var ids = idsElement[0].EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
        var distances = doc.RootElement.TryGetProperty("distances", out var distancesElement)
            ? distancesElement[0].EnumerateArray().Select(x => x.GetSingle()).ToList()
            : [];
        var documents = doc.RootElement.TryGetProperty("documents", out var documentsElement)
            ? documentsElement[0].EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList()
            : [];
        var metadatas = doc.RootElement.TryGetProperty("metadatas", out var metadatasElement)
            ? metadatasElement[0].EnumerateArray().Select(ParseMetadata).ToList()
            : [];

        for (int i = 0; i < ids.Count; i++)
        {
            var distance = i < distances.Count ? distances[i] : 0f;
            var similarity = 1f / (1f + distance);
            if (similarity < minSimilarity)
                continue;

            results.Add(new VectorSearchResult
            {
                Id = ids[i],
                Content = i < documents.Count ? documents[i] : string.Empty,
                Similarity = similarity,
                Metadata = i < metadatas.Count ? metadatas[i] : []
            });
        }

        return results;
    }

    private static Dictionary<string, object> ParseMetadata(JsonElement element)
    {
        var dict = new Dictionary<string, object>();
        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => (object)(prop.Value.GetString() ?? string.Empty),
                JsonValueKind.Number => prop.Value.TryGetInt32(out var intVal) ? intVal : (object)prop.Value.GetSingle(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => prop.Value.ToString()
            };
        }
        return dict;
    }
}
