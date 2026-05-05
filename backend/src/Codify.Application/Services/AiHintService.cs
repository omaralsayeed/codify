using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class AiHintService(IProblemRepository problemRepo, ITutorAgent tutorAgent) : IAiHintService
{
    public async Task<HintResponse> GetHintAsync(
        HintRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (request.HintLevel < HintRequest.MinHintLevel || request.HintLevel > HintRequest.MaxHintLevel)
            throw new ValidationException(
                $"Hint level must be between {HintRequest.MinHintLevel} and {HintRequest.MaxHintLevel}.");

        var problem = await problemRepo.GetByIdWithDetailsAsync(request.ProblemId)
            ?? throw new NotFoundException($"Problem {request.ProblemId} not found.");

        var input = new TutorAgentInput
        {
            ProblemId = problem.Id,
            ProblemTitle = problem.Title,
            ProblemStatement = problem.Statement,
            ConceptTags = problem.ProblemTags.Select(pt => pt.ConceptTag.Name).ToList(),
            HintLevel = request.HintLevel,
            PreviousHints = request.PreviousHints,
            LastSubmissionStatus = request.LastSubmissionStatus,
            AttemptCount = request.AttemptCount ?? 0,
            RetrievedContext = string.Empty,
            StudentCode = request.StudentCode
        };

        return await tutorAgent.GenerateHintAsync(input, cancellationToken);
    }
}
