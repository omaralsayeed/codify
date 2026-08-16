using Codify.Application.DTOs.Admin;
using Codify.Application.DTOs.Problems;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Repositories;

public class ProblemRepository(CodifyDbContext db) : IProblemRepository
{
    public async Task<(IEnumerable<Problem> Items, int TotalCount)> GetAllAsync(
        ProblemFilterRequest filter, bool isInstructor)
    {
        var query = db.Problems
            .Include(p => p.ProblemTags)
                .ThenInclude(pt => pt.ConceptTag)
            .AsQueryable();

        if (!isInstructor)
            query = query.Where(p => p.IsActive);

        if (filter.Difficulty.HasValue)
            query = query.Where(p => p.Difficulty == filter.Difficulty.Value);

        if (!string.IsNullOrWhiteSpace(filter.Tag))
            query = query.Where(p => p.ProblemTags.Any(pt => pt.ConceptTag.Name == filter.Tag));

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(p => p.Title.Contains(filter.Search));

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Problem?> GetByIdWithDetailsAsync(Guid id) =>
        await db.Problems
            .Include(p => p.ProblemTags)
                .ThenInclude(pt => pt.ConceptTag)
            .Include(p => p.TestCases)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Problem?> GetByIdWithTestCasesAsync(Guid id) =>
        await db.Problems
            .Include(p => p.TestCases)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Problem>> GetUntaggedProblemsAsync() =>
        await db.Problems
            .Include(p => p.ProblemTags)
                .ThenInclude(pt => pt.ConceptTag)
            .Where(p => p.IsActive && !p.ProblemTags.Any())
            .ToListAsync();

    public async Task<List<Problem>> GetAllActiveWithTagsAsync() =>
        await db.Problems
            .Include(p => p.ProblemTags)
                .ThenInclude(pt => pt.ConceptTag)
            .Where(p => p.IsActive && !p.IsDeleted)
            .ToListAsync();

    public async Task<int> GetTotalCountAsync() =>
        await db.Problems.CountAsync(p => !p.IsDeleted);

    public async Task<bool> ExistsWithTitleAsync(string title, Guid? excludeId = null) =>
        await db.Problems.AnyAsync(p =>
            !p.IsDeleted &&
            p.Title.ToLower() == title.ToLower() &&
            (excludeId == null || p.Id != excludeId.Value));

    /// <summary>
    /// Admin-only paginated list. Returns ALL non-deleted problems regardless of IsActive.
    /// Supports search by title, filter by difficulty/tag/isActive, sort, and paging.
    /// </summary>
    public async Task<(IReadOnlyList<Problem> Items, int TotalCount)> GetAdminProblemsAsync(
        AdminProblemFilterRequest filter)
    {
        var query = db.Problems
            .Include(p => p.ProblemTags)
                .ThenInclude(pt => pt.ConceptTag)
            .Where(p => !p.IsDeleted)   // soft-deleted are truly gone from all views
            .AsQueryable();

        // Search: case-insensitive title contains
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(p => p.Title.Contains(filter.Search));

        // Difficulty filter: spec sends 0/1/2 as int, domain enum maps the same way
        if (filter.Difficulty.HasValue)
            query = query.Where(p => (int)p.Difficulty == filter.Difficulty.Value);

        // Tag filter
        if (!string.IsNullOrWhiteSpace(filter.Tag))
            query = query.Where(p => p.ProblemTags.Any(pt => pt.ConceptTag.Name == filter.Tag));

        // IsActive filter — null means return both
        if (filter.IsActive.HasValue)
            query = query.Where(p => p.IsActive == filter.IsActive.Value);

        // Sort
        query = (filter.SortBy?.ToLower(), filter.SortDir?.ToLower()) switch
        {
            ("title",       "asc")  => query.OrderBy(p => p.Title),
            ("title",       _)      => query.OrderByDescending(p => p.Title),
            ("difficulty",  "asc")  => query.OrderBy(p => p.Difficulty),
            ("difficulty",  _)      => query.OrderByDescending(p => p.Difficulty),
            ("solvedcount", "asc")  => query.OrderBy(p => p.AcceptedSubmissionsCount),
            ("solvedcount", _)      => query.OrderByDescending(p => p.AcceptedSubmissionsCount),
            ("createdat",   "asc")  => query.OrderBy(p => p.CreatedAt),
            _                       => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync();

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task AddAsync(Problem problem) =>
        await db.Problems.AddAsync(problem);

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
