using Codify.Application.DTOs.Admin;

namespace Codify.Application.Interfaces;

public interface IAdminService
{
    /// <summary>Returns all instructors whose status is Pending.</summary>
    Task<IReadOnlyList<PendingInstructorResponse>> GetPendingInstructorsAsync();

    /// <summary>Approves a pending instructor, enabling them to log in.</summary>
    Task<ApproveInstructorResponse> ApproveInstructorAsync(Guid instructorId, Guid adminId);
}
