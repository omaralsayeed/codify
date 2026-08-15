using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class AiHintService(
    IProblemRepository problemRepo,
    IHintRepository hintRepo,
    ITutorAgent tutorAgent,
    IPerformanceService performanceService) : IAiHintService
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
            UserId = userId,
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

        // Persist the hint log with the agent's decision-making evidence
        var toolsUsedJson = response.ToolsUsed.Count > 0
            ? JsonSerializer.Serialize(response.ToolsUsed)
            : null;

        var hintLog = HintLog.CreateWithAgentMetadata(
            userId: userId,
            problemId: problem.Id,
            hintLevel: nextLevel,
            responseText: response.HintText,
            requestText: request.StudentCode,
            toolsUsedJson: toolsUsedJson,
            reasoningSummary: response.ReasoningSummary,
            modelUsed: response.ModelUsed,
            tokenCount: response.TotalTokens,
            latencyMs: response.LatencyMs);

        await hintRepo.AddAsync(hintLog);
        await hintRepo.SaveChangesAsync();

        // Update performance profile hint count
        await performanceService.IncrementHintCountAsync(userId);

        // Ensure response reflects server-computed level and hasMoreHints
        response.HintLevel = nextLevel;
        response.HasMoreHints = nextLevel < MaxHintLevel;

        return response;
    }

    public async Task<HintHistoryResponse> GetHintHistoryAsync(
        Guid problemId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var hints = (await hintRepo.GetByUserAndProblemAsync(userId, problemId)).ToList();

        return new HintHistoryResponse
        {
            ProblemId = problemId,
            TotalHintsUsed = hints.Count,
            CanRequestMore = hints.Count < MaxHintLevel,
            Hints = hints.Select(h => new HintHistoryItem
            {
                HintLevel = h.HintLevel,
                HintText = h.ResponseText,
                CreatedAt = h.CreatedAt,
                ToolsUsed = string.IsNullOrWhiteSpace(h.ToolsUsedJson)
                    ? []
                    : (JsonSerializer.Deserialize<List<string>>(h.ToolsUsedJson) ?? []),
                ReasoningSummary = h.ReasoningSummary,
                ModelUsed = h.ModelUsed,
                TokenCount = h.TokenCount,
                LatencyMs = h.LatencyMs
            }).ToList()
        };
    }
}
