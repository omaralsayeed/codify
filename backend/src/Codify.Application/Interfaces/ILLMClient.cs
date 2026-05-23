namespace Codify.Application.Interfaces;

public interface ILLMClient
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}
