using Codify.Application.DTOs.Admin;
using Codify.Application.DTOs.Problems;
using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IProblemRepository
{
    Task<(IEnumerable<Problem> Items, int TotalCount)> GetAllAsync(ProblemFilterRequest filter, bool isInstructor);

    /// <summary>Returns a problem by id with no navigation properties loaded. Used for lightweight mutations.</summary>
    Task<Problem?> GetByIdAsync(Guid id);

    Task<Problem?> GetByIdWithDetailsAsync(Guid id);
    Task<Problem?> GetByIdWithTestCasesAsync(Guid id);

    /// <summary>Returns active problems that currently have no concept tags (for the Tagging Agent scan).</summary>
    Task<List<Problem>> GetUntaggedProblemsAsync();

    /// <summary>Returns all active problems with their tags (for RAG ingestion).</summary>
    Task<List<Problem>> GetAllActiveWithTagsAsync();

    /// <summary>Total count of non-deleted problems. Used for admin stats.</summary>
    Task<int> GetTotalCountAsync();

    /// <summary>Returns true if a non-deleted problem with the given title already exists.</summary>
    Task<bool> ExistsWithTitleAsync(string title, Guid? excludeId = null);

    /// <summary>
    /// Admin-only paginated list. Returns ALL problems regardless of IsActive status
    /// (soft-deleted problems are always excluded). Supports search, filter by
    /// difficulty/tag/isActive, sort, and paging.
    /// </summary>
    Task<(IReadOnlyList<Problem> Items, int TotalCount)> GetAdminProblemsAsync(AdminProblemFilterRequest filter);

    Task AddAsync(Problem problem);
    Task SaveChangesAsync();
}
