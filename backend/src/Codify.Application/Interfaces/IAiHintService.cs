using Codify.Application.DTOs.AI;

namespace Codify.Application.Interfaces;

public interface IAiHintService
{
    Task<HintResponse> GetHintAsync(HintRequest request, Guid userId, CancellationToken cancellationToken = default);
}
