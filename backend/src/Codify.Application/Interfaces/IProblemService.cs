using Codify.Application.DTOs;
using Codify.Application.DTOs.Problems;

namespace Codify.Application.Interfaces;

public interface IProblemService
{
    Task<PagedResult<ProblemSummaryResponse>> GetAllAsync(ProblemFilterRequest filter, bool isInstructor);
    Task<ProblemDetailResponse> GetByIdAsync(Guid id);
    Task<ProblemDetailResponse> CreateAsync(CreateProblemRequest request, Guid authorId);
    Task<ProblemDetailResponse> UpdateAsync(Guid id, UpdateProblemRequest request);

    /// <summary>
    /// Toggles a problem's active/inactive state.
    /// Returns the problem's id and new isActive value.
    /// Throws NotFoundException if not found.
    /// </summary>
    Task<(Guid Id, bool IsActive)> SetActiveAsync(Guid id, bool isActive);

    /// <summary>
    /// Soft-deletes a problem. Returns the id and deleted=true.
    /// Throws NotFoundException if not found.
    /// </summary>
    Task<Guid> DeleteAsync(Guid id);
}
