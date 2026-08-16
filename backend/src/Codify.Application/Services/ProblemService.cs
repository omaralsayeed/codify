using System.Text.Json;
using Codify.Application.DTOs;
using Codify.Application.DTOs.Problems;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class ProblemService(
    IProblemRepository problemRepo,
    IConceptTagRepository tagRepo) : IProblemService
{
    public async Task<PagedResult<ProblemSummaryResponse>> GetAllAsync(ProblemFilterRequest filter, bool isInstructor)
    {
        var (items, total) = await problemRepo.GetAllAsync(filter, isInstructor);

        return new PagedResult<ProblemSummaryResponse>
        {
            Items = items.Select(MapToSummary),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ProblemDetailResponse> GetByIdAsync(Guid id)
    {
        var problem = await problemRepo.GetByIdWithDetailsAsync(id)
            ?? throw new NotFoundException($"Problem {id} not found.");

        return MapToDetail(problem);
    }

    public async Task<ProblemDetailResponse> CreateAsync(CreateProblemRequest request, Guid authorId)
    {
        var tags = await tagRepo.GetByIdsAsync(request.TagIds);
        var languageJson = JsonSerializer.Serialize(request.LanguageSupport);

        var problem = Problem.Create(
            request.Title,
            request.Statement,
            request.Difficulty,
            request.Constraints,
            languageJson,
            authorId,
            request.TimeLimitMs,
            request.MemoryLimitMb);

        foreach (var tag in tags)
            problem.ProblemTags.Add(ProblemTag.Create(problem.Id, tag.Id));

        int orderIndex = 0;
        foreach (var tc in request.TestCases)
            problem.TestCases.Add(TestCase.Create(
                problem.Id,
                tc.InputData,
                tc.ExpectedOutput,
                tc.IsSample,
                tc.VisibilityMode,
                orderIndex++));

        await problemRepo.AddAsync(problem);
        await problemRepo.SaveChangesAsync();

        return await GetByIdAsync(problem.Id);
    }

    public async Task<ProblemDetailResponse> UpdateAsync(Guid id, UpdateProblemRequest request)
    {
        var problem = await problemRepo.GetByIdWithDetailsAsync(id)
            ?? throw new NotFoundException($"Problem {id} not found.");

        // Core content fields — only apply what was sent
        var newTitle = request.Title ?? problem.Title;
        var newStatement = request.Statement ?? problem.Statement;
        var newDifficulty = request.Difficulty ?? problem.Difficulty;
        var newConstraints = request.Constraints ?? problem.Constraints;
        var newLanguageJson = request.LanguageSupport is not null
            ? JsonSerializer.Serialize(request.LanguageSupport)
            : problem.LanguageSupportJson;

        problem.Update(newTitle, newStatement, newDifficulty, newConstraints, newLanguageJson);

        // Active state toggle
        if (request.IsActive.HasValue)
            problem.SetActive(request.IsActive.Value);

        // Resource limits
        var newTimeLimitMs   = request.TimeLimitMs   ?? problem.TimeLimitMs;
        var newMemoryLimitMb = request.MemoryLimitMb ?? problem.MemoryLimitMb;
        if (request.TimeLimitMs.HasValue || request.MemoryLimitMb.HasValue)
            problem.UpdateLimits(newTimeLimitMs, newMemoryLimitMb);

        // Tags — clear and replace when provided
        if (request.TagIds is not null)
        {
            problem.ProblemTags.Clear();
            var tags = await tagRepo.GetByIdsAsync(request.TagIds);
            foreach (var tag in tags)
                problem.ProblemTags.Add(ProblemTag.Create(problem.Id, tag.Id));
        }

        // Sample test cases — clear and replace when provided
        if (request.SampleTestCases is not null)
        {
            // Remove existing sample test cases (keep hidden ones intact)
            var existingSamples = problem.TestCases
                .Where(tc => tc.IsSample)
                .ToList();
            foreach (var tc in existingSamples)
                tc.SoftDelete();

            int orderIndex = problem.TestCases.Count(tc => !tc.IsSample);
            foreach (var sample in request.SampleTestCases)
            {
                problem.TestCases.Add(TestCase.Create(
                    problem.Id,
                    sample.Input,
                    sample.ExpectedOutput,
                    isSample: true,
                    TestCaseVisibility.Visible,
                    orderIndex++));
            }
        }

        await problemRepo.SaveChangesAsync();
        return MapToDetail(problem);
    }

    public async Task DeleteAsync(Guid id)
    {
        var problem = await problemRepo.GetByIdWithDetailsAsync(id)
            ?? throw new NotFoundException($"Problem {id} not found.");

        problem.SoftDelete();
        await problemRepo.SaveChangesAsync();
    }

    private static ProblemSummaryResponse MapToSummary(Problem p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        Difficulty = p.Difficulty,
        Tags = p.ProblemTags.Select(pt => pt.ConceptTag.Name).ToList(),
        IsActive = p.IsActive
    };

    private static ProblemDetailResponse MapToDetail(Problem p)
    {
        var languages = JsonSerializer.Deserialize<List<string>>(p.LanguageSupportJson) ?? [];
        return new ProblemDetailResponse
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            Statement = p.Statement,
            Difficulty = p.Difficulty,
            Constraints = p.Constraints,
            LanguageSupport = languages,
            Tags = p.ProblemTags.Select(pt => pt.ConceptTag.Name).ToList(),
            IsActive = p.IsActive,
            IsPublic = p.IsPublic,
            TimeLimitMs = p.TimeLimitMs,
            MemoryLimitMb = p.MemoryLimitMb,
            AcceptedSubmissionsCount = p.AcceptedSubmissionsCount,
            TotalSubmissionsCount = p.TotalSubmissionsCount,
            SampleTestCases = p.TestCases
                .Where(tc => tc.IsSample)
                .OrderBy(tc => tc.OrderIndex)
                .Select(tc => new SampleTestCaseResponse
                {
                    Input = tc.InputData,
                    ExpectedOutput = tc.ExpectedOutput
                }).ToList()
        };
    }
}
