using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class AiHintService(
    IProblemRepository problemRepo,
    IHintLogRepository hintLogRepo,
    ITutorAgent tutorAgent) : IAiHintService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
            StudentCode = request.StudentCode,
            UserId = userId
        };

        var response = await tutorAgent.GenerateHintAsync(input, cancellationToken);

        // Persist the hint to HintLog with agentic metadata (tools used + reasoning).
        var toolsUsedJson = JsonSerializer.Serialize(response.ToolsUsed, JsonOptions);
        var log = HintLog.CreateWithAgentMetadata(
            userId,
            problem.Id,
            response.HintLevel,
            response.HintText,
            requestText: request.StudentCode is null ? null : $"level={request.HintLevel}",
            toolsUsedJson,
            response.ReasoningSummary ?? string.Empty);
        await hintLogRepo.AddAsync(log);
        await hintLogRepo.SaveChangesAsync();

        return response;
    }
}
