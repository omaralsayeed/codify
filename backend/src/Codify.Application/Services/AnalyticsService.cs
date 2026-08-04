using System.Text.Json;
using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Codify.Application.Services;

/// <summary>
/// Orchestrates the Analytics / Tagging Agent: loads available topics, delegates
/// to the .NET-native agent, upserts the PerformanceProfile, and returns the
/// structured response.
/// </summary>
public class AnalyticsService(
    IConceptTagRepository tagRepo,
    IPerformanceProfileRepository profileRepo,
    IAnalyticsAgent analyticsAgent,
    ILogger<AnalyticsService> logger) : IAnalyticsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<AnalyticsResponse> AnalyzeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tags = await tagRepo.GetAllAsync();
        var availableTopics = tags.Select(t => t.Name).ToList();

        var input = new AnalyticsAgentInput
        {
            UserId = userId,
            AvailableTopics = availableTopics
        };

        AnalyticsResult result;
        try
        {
            result = await analyticsAgent.AnalyzeAsync(input, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Analytics agent call failed for user {UserId}.", userId);
            result = CreateFallbackResult();
        }

        await UpsertProfileAsync(userId, result);
        return MapToResponse(userId, result);
    }

    public async Task<AnalyticsResponse?> GetAnalyticsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await profileRepo.GetByUserIdAsync(userId);
        if (profile is null)
            return null;

        if (!string.IsNullOrWhiteSpace(profile.AnalyticsJson) && profile.AnalyticsJson != "{}")
        {
            try
            {
                var response = JsonSerializer.Deserialize<AnalyticsResponse>(profile.AnalyticsJson, JsonOptions);
                if (response is not null)
                {
                    response.UserId = userId;
                    response.LastUpdatedAt = profile.LastUpdatedAt;
                    return response;
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to deserialize AnalyticsJson for user {UserId}.", userId);
            }
        }

        return new AnalyticsResponse
        {
            UserId = userId,
            LearningStage = profile.LearningStage,
            OverallScore = profile.OverallScore,
            Consistency = profile.Consistency,
            Confidence = profile.Confidence,
            RecommendedProblemDifficulty = profile.RecommendedDifficulty,
            SuccessRate = profile.SuccessRate,
            AverageAttempts = profile.AverageAttempts,
            LastUpdatedAt = profile.LastUpdatedAt
        };
    }

    private async Task UpsertProfileAsync(Guid userId, AnalyticsResult result)
    {
        var profile = await profileRepo.GetByUserIdAsync(userId);
        var weakJson = JsonSerializer.Serialize(result.WeakTopics);
        var strongJson = JsonSerializer.Serialize(result.StrongTopics);
        var fullJson = JsonSerializer.Serialize(MapToResponse(userId, result), JsonOptions);

        if (profile is null)
        {
            profile = PerformanceProfile.CreateForUser(userId);
            profile.UpdateAnalytics(weakJson, strongJson, result.SuccessRate, result.AverageAttempts,
                result.LearningStage, result.OverallScore, result.Consistency, result.Confidence,
                result.RecommendedProblemDifficulty, fullJson);
            await profileRepo.AddAsync(profile);
        }
        else
        {
            profile.UpdateAnalytics(weakJson, strongJson, result.SuccessRate, result.AverageAttempts,
                result.LearningStage, result.OverallScore, result.Consistency, result.Confidence,
                result.RecommendedProblemDifficulty, fullJson);
        }

        await profileRepo.SaveChangesAsync();
    }

    private static AnalyticsResponse MapToResponse(Guid userId, AnalyticsResult r) => new()
    {
        UserId = userId,
        LearningStage = r.LearningStage,
        OverallScore = r.OverallScore,
        Consistency = r.Consistency,
        Confidence = r.Confidence,
        WeakTopics = r.WeakTopics,
        StrongTopics = r.StrongTopics,
        ImprovingTopics = r.ImprovingTopics,
        DecliningTopics = r.DecliningTopics,
        CommonMistakes = r.CommonMistakes,
        RecommendedTopics = r.RecommendedTopics,
        RecommendedProblemDifficulty = r.RecommendedProblemDifficulty,
        PracticePlan = r.PracticePlan.Select(p => new AnalyticsPracticePlanItem
        {
            Topic = p.Topic, Action = p.Action, Priority = p.Priority
        }).ToList(),
        Summary = r.Summary,
        SuccessRate = r.SuccessRate,
        AverageAttempts = r.AverageAttempts,
        ToolsUsed = r.ToolsUsed,
        ReasoningSummary = r.ReasoningSummary
    };

    private static AnalyticsResult CreateFallbackResult() => new()
    {
        Summary = "Analytics could not be completed at this time. Please try again later."
    };
}
