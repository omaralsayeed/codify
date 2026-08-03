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
