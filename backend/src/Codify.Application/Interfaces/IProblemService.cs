using Codify.Application.DTOs;
using Codify.Application.DTOs.Problems;

namespace Codify.Application.Interfaces;

public interface IProblemService
{
    Task<PagedResult<ProblemSummaryResponse>> GetAllAsync(ProblemFilterRequest filter, bool isInstructor);
    Task<ProblemDetailResponse> GetByIdAsync(Guid id);
    Task<ProblemDetailResponse> CreateAsync(CreateProblemRequest request);
    Task<ProblemDetailResponse> UpdateAsync(Guid id, UpdateProblemRequest request);
    Task DeleteAsync(Guid id);
}
