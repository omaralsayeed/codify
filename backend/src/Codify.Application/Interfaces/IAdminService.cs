using Codify.Application.DTOs.Admin;

namespace Codify.Application.Interfaces;

public interface IAdminService
{
    /// <summary>Returns all instructors whose status is Pending.</summary>
    Task<IReadOnlyList<PendingInstructorResponse>> GetPendingInstructorsAsync();

    /// <summary>Approves a pending instructor, enabling them to log in.</summary>
    Task<ApproveInstructorResponse> ApproveInstructorAsync(Guid instructorId, Guid adminId);

    /// <summary>Returns real platform-wide aggregate statistics.</summary>
    Task<AdminStatsResponse> GetStatsAsync();

    /// <summary>Returns all users across all roles from the database.</summary>
    Task<IReadOnlyList<AdminUserListItemResponse>> GetAllUsersAsync();

    /// <summary>Returns full user details with submission history.</summary>
    Task<AdminUserDetailResponse> GetUserDetailAsync(Guid id);

    /// <summary>Updates a user's status (Active, Suspended, Rejected, etc.).</summary>
    Task<bool> UpdateUserStatusAsync(Guid id, Codify.Domain.Enums.UserStatus newStatus);
}
