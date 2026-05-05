using Microsoft.Extensions.Logging;

namespace Codify.Infrastructure.AI;

public class PromptLoader(ILogger<PromptLoader> logger) : IPromptLoader
{
    private readonly string _promptRoot = Path.Combine(AppContext.BaseDirectory, "AI", "Prompts");
    private readonly ILogger<PromptLoader> _logger = logger;

    public async Task<string> LoadAsync(string promptFileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(promptFileName))
            throw new ArgumentException("Prompt file name is required.", nameof(promptFileName));

        var path = Path.Combine(_promptRoot, promptFileName);
        if (!File.Exists(path))
        {
            _logger.LogError("Prompt file not found: {PromptPath}", path);
            throw new FileNotFoundException($"Prompt file not found: {path}", path);
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }
}
