using System.Net.Http.Json;
using System.Text.Json;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Codify.Infrastructure.AI;

/// <summary>
/// Generates text embeddings via the OpenAI REST API
/// (https://api.openai.com/v1/embeddings) using the model configured in
/// OpenAiOptions.EmbeddingModel (defaults to text-embedding-3-small).
/// </summary>
public class OpenAiEmbeddingService(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiEmbeddingService> logger) : IEmbeddingService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly OpenAiOptions _options = options.Value;
    private readonly ILogger<OpenAiEmbeddingService> _logger = logger;

    public async Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        var results = await GenerateBatchAsync([text], cancellationToken);
        return results.Count > 0 ? results[0] : [];
    }

    public async Task<List<float[]>> GenerateBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textList = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (textList.Count == 0)
            return [];

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "OpenAI:ApiKey is not configured; cannot generate embeddings.");

        var model = string.IsNullOrWhiteSpace(_options.EmbeddingModel)
            ? OpenAiOptions.DefaultEmbeddingModel
            : _options.EmbeddingModel;

        var payload = new { model, input = textList };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("embeddings", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            var vectors = new List<float[]>();
            foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                var embedding = item.GetProperty("embedding").EnumerateArray()
                    .Select(x => x.GetSingle())
                    .ToArray();
                vectors.Add(embedding);
            }

            return vectors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI embedding generation failed (model={Model}).", model);
            throw;
        }
    }
}
