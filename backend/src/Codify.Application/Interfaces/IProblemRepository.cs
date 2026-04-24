using Codify.Application.DTOs.Problems;
using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IProblemRepository
{
    Task<(IEnumerable<Problem> Items, int TotalCount)> GetAllAsync(ProblemFilterRequest filter, bool isInstructor);
    Task<Problem?> GetByIdWithDetailsAsync(Guid id);
    Task<Problem?> GetByIdWithTestCasesAsync(Guid id);
    Task AddAsync(Problem problem);
    Task SaveChangesAsync();
}
