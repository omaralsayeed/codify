namespace Codify.Infrastructure.AI;

public class OpenAiOptions
{
    public const string SectionName = "OpenAI";
    public const string DefaultModel = "gpt-4o";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = DefaultModel;
}
