namespace Codify.Infrastructure.AI;

public class OpenAiOptions
{
    public const string SectionName = "OpenAI";
    public const string DefaultModel = "gpt-4o";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = DefaultModel;

    /// <summary>Embedding model used for RAG (e.g. text-embedding-3-small).</summary>
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}
