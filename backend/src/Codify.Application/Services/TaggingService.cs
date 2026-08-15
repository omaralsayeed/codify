using Codify.Application.Agents;
using Codify.Application.DTOs.AI;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Codify.Application.Services;

/// <summary>
/// Orchestrates the Tagging Agent. The agent classifies a problem's concept tags
/// (RAG-grounded); this service loads the problem, applies the validated tags, and
/// exposes the automatic scan plus the on-progress user-tag refresh.
/// </summary>
public class TaggingService(
    ITaggingAgent taggingAgent,
    IProblemRepository problemRepo,
    IConceptTagRepository conceptTagRepo,
    IPerformanceService performanceService,
    ILogger<TaggingService> logger) : ITaggingService
{
    public async Task<TagProblemResponse> TagProblemAsync(Guid problemId, CancellationToken cancellationToken = default)
    {
        var problem = await problemRepo.GetByIdWithDetailsAsync(problemId)
            ?? throw new NotFoundException($"Problem {problemId} not found.");

        if (problem.ProblemTags.Count > 0)
        {
            return new TagProblemResponse
            {
                ProblemId = problemId,
                AlreadyTagged = true,
                AssignedTags = problem.ProblemTags
                    .Select(pt => pt.ConceptTag?.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Select(name => name!)
                    .ToList(),
                Reasoning = "Problem already has tags; left unchanged."
            };
        }

        var allTags = (await conceptTagRepo.GetAllAsync()).ToList();
        return await ClassifyAndApplyAsync(problem, allTags, cancellationToken);
    }

    public async Task<TagScanResponse> TagAllUntaggedProblemsAsync(CancellationToken cancellationToken = default)
    {
        var untagged = await problemRepo.GetUntaggedProblemsAsync();
        var allTags = (await conceptTagRepo.GetAllAsync()).ToList();

        var scan = new TagScanResponse { UntaggedFound = untagged.Count };

        foreach (var problem in untagged)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await ClassifyAndApplyAsync(problem, allTags, cancellationToken);
                scan.Results.Add(result);
                if (!result.AlreadyTagged && result.AssignedTags.Count > 0)
                    scan.Tagged++;
            }
            catch (Exception ex)
            {
                // A single problem failing must not abort the whole scan.
                logger.LogError(ex, "Failed to auto-tag problem {ProblemId}.", problem.Id);
            }
        }

        logger.LogInformation("Tagging scan complete: {Tagged}/{Found} untagged problems tagged.",
            scan.Tagged, scan.UntaggedFound);

        return scan;
    }

    public async Task UpdateUserTagsOnProgressAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Reuse the existing deterministic weak/strong-topic recomputation.
        await performanceService.UpdateAfterSubmissionAsync(userId);
    }

    public async Task TagOnSubmissionAsync(Guid problemId, Guid userId, CancellationToken cancellationToken = default)
    {
        // 1. Tag the just-submitted problem if it is untagged.
        try
        {
            await TagProblemAsync(problemId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to tag submitted problem {ProblemId}.", problemId);
        }

        // 2. Scan and tag all other untagged problems.
        try
        {
            await TagAllUntaggedProblemsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to scan and tag untagged problems after submission.");
        }

        // 3. Refresh the student's weak/strong topic profile.
        try
        {
            await UpdateUserTagsOnProgressAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update user tags on progress after submission.");
        }
    }

    // ── Private ───────────────────────────────────────────────────

    private async Task<TagProblemResponse> ClassifyAndApplyAsync(
        Problem problem, List<ConceptTag> allTags, CancellationToken cancellationToken)
    {
        var availableTagNames = allTags.Select(t => t.Name).ToList();

        var classification = await taggingAgent.ClassifyProblemTagsAsync(new TaggingAgentInput
        {
            ProblemTitle = problem.Title,
            ProblemStatement = problem.Statement,
            AvailableTags = availableTagNames
        }, cancellationToken);

        if (classification.AssignedTags.Count == 0)
        {
            logger.LogInformation("Tagging agent assigned no tags for problem {ProblemId}: {Reason}",
                problem.Id, classification.Reasoning);

            return new TagProblemResponse
            {
                ProblemId = problem.Id,
                AssignedTags = [],
                Confidence = classification.Confidence,
                Reasoning = classification.Reasoning
            };
        }

        // Map the validated tag names back to ConceptTag entities and apply them.
        var tagsByName = allTags.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        var applied = new List<string>();

        foreach (var tagName in classification.AssignedTags)
        {
            if (!tagsByName.TryGetValue(tagName, out var conceptTag))
                continue;

            // Skip if this tag is already attached to the problem.
            if (problem.ProblemTags.Any(pt => pt.ConceptTagId == conceptTag.Id))
                continue;

            await conceptTagRepo.AddProblemTagAsync(ProblemTag.Create(problem.Id, conceptTag.Id));
            applied.Add(conceptTag.Name);
        }

        if (applied.Count > 0)
            await conceptTagRepo.SaveChangesAsync();

        return new TagProblemResponse
        {
            ProblemId = problem.Id,
            AssignedTags = applied,
            Confidence = classification.Confidence,
            Reasoning = classification.Reasoning
        };
    }
}
