using Codify.Application.DTOs.Problems;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
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

    public async Task AddAsync(Problem problem) =>
        await db.Problems.AddAsync(problem);

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
