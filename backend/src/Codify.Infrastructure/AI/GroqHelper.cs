using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Codify.Infrastructure.AI;

/// <summary>
/// Dead simple Groq fallback - just makes HTTP calls when needed
/// </summary>
public static class GroqHelper
{
    public static async Task<string> CallGroqAsync(
        string apiKey,
        string systemPrompt,
        string userMessage,
        string model = "openai/gpt-oss-20b",
        CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        client.Timeout = TimeSpan.FromSeconds(30);
        
        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = 0.7,
            max_tokens = 2000,
            // Explicitly tell Groq NOT to use tool calling
            response_format = new { type = "text" }
        };

        var response = await client.PostAsJsonAsync(
            "https://api.groq.com/openai/v1/chat/completions",
            payload,
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<GroqResponse>(json);
        
        return result?.Choices?[0]?.Message?.Content ?? string.Empty;
    }

    private class GroqResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
