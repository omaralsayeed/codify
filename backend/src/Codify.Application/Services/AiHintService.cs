using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class AiHintService(
    IProblemRepository problemRepo,
    IHintRepository hintRepo,
    ITutorAgent tutorAgent) : IAiHintService
{
    private const int MaxHintLevel = HintRequest.MaxHintLevel;

    public async Task<HintResponse> GetHintAsync(
        HintRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var problem = await problemRepo.GetByIdWithDetailsAsync(request.ProblemId)
            ?? throw new NotFoundException($"Problem {request.ProblemId} not found.");

        // Determine next hint level from persisted history (ignore client-supplied level)
        var currentLevel = await hintRepo.GetCurrentHintLevelAsync(userId, problem.Id);
        if (currentLevel >= MaxHintLevel)
            throw new ValidationException(
                $"You have already used all {MaxHintLevel} hints for this problem.");

        var nextLevel = currentLevel + 1;

        // Fetch previous hint texts to give the agent context
        var previousHints = (await hintRepo.GetByUserAndProblemAsync(userId, problem.Id))
            .Select(h => h.ResponseText)
            .ToList();

        var input = new TutorAgentInput
        {
            ProblemId = problem.Id,
            ProblemTitle = problem.Title,
            ProblemStatement = problem.Statement,
            ConceptTags = problem.ProblemTags.Select(pt => pt.ConceptTag.Name).ToList(),
            HintLevel = nextLevel,
            PreviousHints = previousHints,
            LastSubmissionStatus = request.LastSubmissionStatus,
            AttemptCount = request.AttemptCount ?? 0,
            RetrievedContext = string.Empty,
            StudentCode = request.StudentCode
        };

        var response = await tutorAgent.GenerateHintAsync(input, cancellationToken);

        // Persist the hint log
        var hintLog = HintLog.Create(
            userId: userId,
            problemId: problem.Id,
            hintLevel: nextLevel,
            responseText: response.HintText,
            requestText: request.StudentCode);

        await hintRepo.AddAsync(hintLog);
        await hintRepo.SaveChangesAsync();

        // Ensure response reflects server-computed level and hasMoreHints
        response.HintLevel = nextLevel;
        response.HasMoreHints = nextLevel < MaxHintLevel;

        return response;
    }
}
