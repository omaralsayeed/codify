using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Codify.Infrastructure.AI;

/// <summary>
/// Chroma Cloud vector store client (Chroma v2 REST API).
///
/// Chroma Cloud is a managed, distributed Chroma. The tenant and database are
/// part of the URL path and authentication is a Bearer API key. All source
/// documents are assumed to be pre-populated in the configured collection;
/// this client reads them at agent execution time (retrieval) and can upsert.
///
/// Every public method degrades gracefully: on any connection/HTTP failure the
/// search returns an empty list (and logs a warning) so a Chroma outage never
/// breaks the agents.
/// </summary>
public class ChromaCloudVectorStore(
    HttpClient httpClient,
    IOptions<ChromaCloudOptions> options,
    ILogger<ChromaCloudVectorStore> logger) : IVectorStore
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ChromaCloudOptions _options = options.Value;
    private readonly ILogger<ChromaCloudVectorStore> _logger = logger;

    // Resolved once and cached; collection operations are addressed by id.
    private string? _collectionId;

    public async Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
        => await GetCollectionIdAsync(cancellationToken);

    public async Task UpsertAsync(IReadOnlyList<VectorDocument> documents, CancellationToken cancellationToken = default)
    {
        if (documents.Count == 0) return;

        try
        {
            var collectionId = await GetCollectionIdAsync(cancellationToken);
            var url = $"{CollectionsBasePath()}/{collectionId}/upsert";

            var payload = new
            {
                ids = documents.Select(d => d.Id).ToList(),
                embeddings = documents.Select(d => d.Vector).ToList(),
                metadatas = documents.Select(d => d.Metadata).ToList(),
                documents = documents.Select(d => d.Content).ToList()
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Chroma upsert failed: {Status} {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chroma upsert failed for {Count} document(s).", documents.Count);
        }
    }

    public async Task<List<VectorSearchResult>> SearchAsync(
        float[] queryVector,
        string? conceptTag = null,
        string? source = null,
        int topK = 5,
        float? minSimilarity = null,
        CancellationToken cancellationToken = default)
    {
        if (queryVector.Length == 0)
            return [];

        try
        {
            var collectionId = await GetCollectionIdAsync(cancellationToken);
            var url = $"{CollectionsBasePath()}/{collectionId}/query";

            var payload = new Dictionary<string, object>
            {
                ["query_embeddings"] = new[] { queryVector },
                ["n_results"] = topK,
                ["include"] = new[] { "documents", "metadatas", "distances" }
            };

            var where = BuildWhereClause(conceptTag, source);
            if (where is not null)
                payload["where"] = where;

            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Chroma query failed: {Status} {Body}", response.StatusCode, body);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseQueryResponse(json, minSimilarity ?? _options.SimilarityThreshold);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chroma search failed; returning empty results.");
            return [];
        }
    }

    // ── Private ───────────────────────────────────────────────────

    private string TenantDatabasePath() =>
        $"/api/v2/tenants/{Uri.EscapeDataString(_options.Tenant)}/databases/{Uri.EscapeDataString(_options.Database)}";

    private string CollectionsBasePath() => $"{TenantDatabasePath()}/collections";

    /// <summary>
    /// Resolves the configured collection's id, creating the collection if it
    /// does not exist yet (get-or-create semantics).
    /// </summary>
    private async Task<string> GetCollectionIdAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_collectionId))
            return _collectionId;

        // 1. Try to fetch the collection by name.
        var getUrl = $"{CollectionsBasePath()}/{Uri.EscapeDataString(_options.CollectionName)}";
        var getResponse = await _httpClient.GetAsync(getUrl, cancellationToken);

        if (getResponse.IsSuccessStatusCode)
        {
            var json = await getResponse.Content.ReadAsStringAsync(cancellationToken);
            _collectionId = ExtractCollectionId(json);
            if (!string.IsNullOrEmpty(_collectionId))
                return _collectionId;
        }

        // 2. Not found — create it.
        var createPayload = new { name = _options.CollectionName, get_or_create = true };
        var createResponse = await _httpClient.PostAsJsonAsync(CollectionsBasePath(), createPayload, cancellationToken);

        if (!createResponse.IsSuccessStatusCode && createResponse.StatusCode != HttpStatusCode.Conflict)
        {
            var body = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Chroma collection create returned {Status}: {Body}", createResponse.StatusCode, body);
        }

        var createJson = await createResponse.Content.ReadAsStringAsync(cancellationToken);
        _collectionId = ExtractCollectionId(createJson);

        if (string.IsNullOrEmpty(_collectionId))
            throw new InvalidOperationException(
                $"Could not resolve Chroma collection id for '{_options.CollectionName}'.");

        return _collectionId;
    }

    private static string? ExtractCollectionId(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("id", out var idElement))
                return idElement.GetString();
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Dictionary<string, object>? BuildWhereClause(string? conceptTag, string? source)
    {
        var filters = new List<Dictionary<string, object>>();

        if (!string.IsNullOrWhiteSpace(conceptTag))
            filters.Add(new Dictionary<string, object> { ["concept_tag"] = conceptTag });
        if (!string.IsNullOrWhiteSpace(source))
            filters.Add(new Dictionary<string, object> { ["source"] = source });

        return filters.Count switch
        {
            0 => null,
            1 => filters[0],
            _ => new Dictionary<string, object> { ["$and"] = filters }
        };
    }

    /// <summary>
    /// Chroma query responses are batch-shaped: every field is an array (one
    /// entry per input query) of arrays (one entry per result). We send a
    /// single query, so we read index 0 of each.
    /// </summary>
    private static List<VectorSearchResult> ParseQueryResponse(string json, float minSimilarity)
    {
        var results = new List<VectorSearchResult>();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("ids", out var idsElement) || idsElement.GetArrayLength() == 0)
            return results;

        var ids = idsElement[0].EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
        var distances = ReadFirstBatchOfFloats(root, "distances");
        var documents = ReadFirstBatchOfStrings(root, "documents");
        var metadatas = ReadFirstBatchOfMetadata(root, "metadatas");

        for (int i = 0; i < ids.Count; i++)
        {
            var distance = i < distances.Count ? Math.Max(distances[i], 0f) : 0f;

            // Map an unbounded distance into a (0,1] similarity so callers can
            // apply a stable threshold regardless of the collection's distance
            // metric (L2 or cosine).
            var similarity = 1f / (1f + distance);
            if (similarity < minSimilarity)
                continue;

            results.Add(new VectorSearchResult
            {
                Id = ids[i],
                Content = i < documents.Count ? documents[i] : string.Empty,
                Similarity = similarity,
                Metadata = i < metadatas.Count ? metadatas[i] : new Dictionary<string, object>()
            });
        }

        return results;
    }

    private static List<float> ReadFirstBatchOfFloats(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var element) || element.GetArrayLength() == 0)
            return [];
        return element[0].EnumerateArray().Select(x => x.GetSingle()).ToList();
    }

    private static List<string> ReadFirstBatchOfStrings(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var element) || element.GetArrayLength() == 0)
            return [];
        return element[0].EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
    }

    private static List<Dictionary<string, object>> ReadFirstBatchOfMetadata(JsonElement root, string property)
    {
        var list = new List<Dictionary<string, object>>();
        if (!root.TryGetProperty(property, out var element) || element.GetArrayLength() == 0)
            return list;

        foreach (var item in element[0].EnumerateArray())
            list.Add(ParseMetadataObject(item));

        return list;
    }

    private static Dictionary<string, object> ParseMetadataObject(JsonElement element)
    {
        var dict = new Dictionary<string, object>();
        if (element.ValueKind != JsonValueKind.Object)
            return dict;

        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                JsonValueKind.Number when prop.Value.TryGetInt32(out var i) => i,
                JsonValueKind.Number => prop.Value.GetSingle(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => prop.Value.ToString()
            };
        }

        return dict;
    }
}
