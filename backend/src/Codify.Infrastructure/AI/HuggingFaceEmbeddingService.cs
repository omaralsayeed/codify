using System.Net.Http.Json;
using System.Text.Json;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Codify.Infrastructure.AI;

/// <summary>
/// Generates text embeddings via the HuggingFace Inference API feature-extraction pipeline.
/// Uses BAAI/bge-small-en-v1.5 model (384 dimensions) by default.
/// 
/// The API returns pooled sentence embeddings directly as List&lt;List&lt;float&gt;&gt; where:
/// - Outer list: one entry per input text
/// - Inner list: the 384-dimensional embedding vector
/// </summary>
public class HuggingFaceEmbeddingService(
    HttpClient httpClient,
    IOptions<EmbeddingApiOptions> options,
    ILogger<HuggingFaceEmbeddingService> logger) : IEmbeddingService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly EmbeddingApiOptions _options = options.Value;
    private readonly ILogger<HuggingFaceEmbeddingService> _logger = logger;

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await EmbedBatchAsync(new[] { text }, cancellationToken);
        return result.Count > 0 ? result[0] : Array.Empty<float>();
    }

    public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
        => EmbedAsync(text, cancellationToken);

    public async Task<List<float[]>> GenerateBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
        => await EmbedBatchAsync(texts, cancellationToken);

    public async Task<List<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        if (textList.Count == 0)
            return new List<float[]>();

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "EmbeddingApi:ApiKey is not configured; cannot generate embeddings.");

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException(
                "EmbeddingApi:BaseUrl is not configured; cannot generate embeddings.");

        try
        {
            var payload = new { inputs = textList.ToArray() };

            // POST to the feature-extraction endpoint (BaseUrl already contains the full path)
            var response = await _httpClient.PostAsJsonAsync(string.Empty, payload, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "HuggingFace embedding API returned {StatusCode}: {Error}",
                    response.StatusCode,
                    errorBody);
                throw new InvalidOperationException(
                    $"HuggingFace embedding API failed with status {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var vectors = ParseEmbeddingResponse(json);

            if (vectors.Count != textList.Count)
            {
                _logger.LogWarning(
                    "Expected {Expected} embeddings but got {Actual}",
                    textList.Count,
                    vectors.Count);
            }

            return vectors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HuggingFace embedding generation failed for {Count} text(s).", textList.Count);
            throw;
        }
    }

    /// <summary>
    /// Parses the HuggingFace feature-extraction response.
    /// Expected format: [[float, float, ...], [float, float, ...], ...]
    /// where each inner array is a 384-dimensional embedding.
    /// </summary>
    private List<float[]> ParseEmbeddingResponse(string json)
    {
        var vectors = new List<float[]>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Response is an array of arrays: [[emb1], [emb2], ...]
            if (root.ValueKind != JsonValueKind.Array)
            {
                _logger.LogError("Expected array response but got {Kind}", root.ValueKind);
                throw new InvalidOperationException("Unexpected response format from HuggingFace API");
            }

            foreach (var embeddingElement in root.EnumerateArray())
            {
                if (embeddingElement.ValueKind == JsonValueKind.Array)
                {
                    var embedding = embeddingElement.EnumerateArray()
                        .Select(x => x.GetSingle())
                        .ToArray();
                    vectors.Add(embedding);
                }
                else
                {
                    _logger.LogWarning("Unexpected embedding element type: {Kind}", embeddingElement.ValueKind);
                }
            }

            return vectors;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse HuggingFace embedding response: {Json}", json);
            throw new InvalidOperationException("Failed to parse embedding response", ex);
        }
    }
}
