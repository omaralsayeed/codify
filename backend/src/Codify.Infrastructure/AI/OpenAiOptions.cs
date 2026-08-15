namespace Codify.Infrastructure.AI;

public class OpenAiOptions
{
    public const string SectionName = "OpenAI";
    public const string DefaultModel = "gpt-4o";
    public const string DefaultEmbeddingModel = "text-embedding-3-small";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = DefaultModel;

    /// <summary>Model used to generate embeddings for the Chroma Cloud RAG layer.</summary>
    public string EmbeddingModel { get; set; } = DefaultEmbeddingModel;

    /// <summary>
    /// Custom base URL for OpenAI API calls. If empty, uses the official OpenAI API.
    /// Example: "http://apiaccess.iti.net.eg/api/v1/student/chat"
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}
