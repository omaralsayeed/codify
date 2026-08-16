using Codify.Application.DTOs.Problems;
using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IProblemRepository
{
    Task<(IEnumerable<Problem> Items, int TotalCount)> GetAllAsync(ProblemFilterRequest filter, bool isInstructor);
    Task<Problem?> GetByIdWithDetailsAsync(Guid id);
    Task<Problem?> GetByIdWithTestCasesAsync(Guid id);

    /// <summary>Returns active problems that currently have no concept tags (for the Tagging Agent scan).</summary>
    Task<List<Problem>> GetUntaggedProblemsAsync();

    /// <summary>Returns all active problems with their tags (for RAG ingestion).</summary>
    Task<List<Problem>> GetAllActiveWithTagsAsync();

    /// <summary>Total count of non-deleted problems. Used for admin stats.</summary>
    Task<int> GetTotalCountAsync();

    Task AddAsync(Problem problem);
    Task SaveChangesAsync();
}
