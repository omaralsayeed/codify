using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Repositories;

// This is the ONLY place that knows about EF Core and SQL.
// The Application layer just calls the interface methods and stays database-agnostic.
public class ConceptTagRepository(CodifyDbContext db) : IConceptTagRepository
{
    // ── ConceptTag queries ──────────────────────────────────────────────

    public async Task<IEnumerable<ConceptTag>> GetAllAsync() =>
        // The global query filter on ConceptTag automatically adds WHERE IsDeleted = 0
        await db.ConceptTags.OrderBy(t => t.Name).ToListAsync();

    public async Task<IEnumerable<ConceptTag>> GetByIdsAsync(IEnumerable<Guid> ids) =>
        await db.ConceptTags.Where(t => ids.Contains(t.Id)).ToListAsync();

    public async Task<ConceptTag?> GetByIdAsync(Guid id) =>
        await db.ConceptTags.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<ConceptTag?> GetByNameAsync(string name) =>
        // Case-insensitive check — important for preventing "DP" and "dp" as separate tags
        await db.ConceptTags.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

    public async Task AddAsync(ConceptTag tag) =>
        await db.ConceptTags.AddAsync(tag);

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();

    // ── ProblemTag (join table) operations ─────────────────────────────

    public async Task<IEnumerable<ProblemTag>> GetTagsByProblemIdAsync(Guid problemId) =>
        // Include the ConceptTag so the service can map it to a DTO in one query
        await db.ProblemTags
            .Include(pt => pt.ConceptTag)
            .Where(pt => pt.ProblemId == problemId)
            .ToListAsync();

    public async Task<ProblemTag?> GetProblemTagAsync(Guid problemId, Guid conceptTagId) =>
        await db.ProblemTags
            .FirstOrDefaultAsync(pt => pt.ProblemId == problemId && pt.ConceptTagId == conceptTagId);

    public async Task AddProblemTagAsync(ProblemTag problemTag) =>
        await db.ProblemTags.AddAsync(problemTag);

    // Note: Remove is synchronous — EF Core marks the entity for deletion immediately.
    // SaveChangesAsync() is called separately by the service to commit the transaction.
    public void RemoveProblemTag(ProblemTag problemTag) =>
        db.ProblemTags.Remove(problemTag);
}
