using System.Net.Http.Json;
using System.Text.Json;
using Codify.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Codify.Infrastructure.AI;

/// <summary>
/// Generates text embeddings via the OpenAI REST API
/// (https://api.openai.com/v1/embeddings). Uses the model configured in
/// OpenAiOptions (defaults to text-embedding-3-small).
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

        var payload = new
        {
            model = _options.EmbeddingModel ?? "text-embedding-3-small",
            input = textList
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "embeddings", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(json);

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
            _logger.LogError(ex, "OpenAI embedding generation failed.");
            throw;
        }
    }
}
