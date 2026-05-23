using Codify.Application.DTOs.AI;

namespace Codify.Application.Agents;

public interface ITutorAgent
{
    Task<HintResponse> GenerateHintAsync(TutorAgentInput input, CancellationToken cancellationToken = default);
}
