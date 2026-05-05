namespace Codify.Infrastructure.AI;

public interface IPromptLoader
{
    Task<string> LoadAsync(string promptFileName, CancellationToken cancellationToken = default);
}
