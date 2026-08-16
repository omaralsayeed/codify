using Codify.Application.DTOs.Admin;

namespace Codify.Application.Interfaces;

public interface IAdminService
{
    /// <summary>Returns all instructors whose status is Pending.</summary>
    Task<IReadOnlyList<PendingInstructorResponse>> GetPendingInstructorsAsync();

    /// <summary>Approves a pending instructor, enabling them to log in.</summary>
    Task<ApproveInstructorResponse> ApproveInstructorAsync(Guid instructorId, Guid adminId);

    // ── Admin panel endpoints ─────────────────────────────────────────────────

    /// <summary>Platform-wide statistics for the admin overview dashboard.</summary>
    Task<AdminStatsResponse> GetStatsAsync();

    /// <summary>Paginated, filterable list of all non-admin users.</summary>
    Task<(IReadOnlyList<AdminUserRow> Users, int Total)> GetUsersAsync(AdminUserFilterRequest filter);

    /// <summary>
    /// Full detail for a single non-admin user including recent submissions.
    /// Throws NotFoundException if the user doesn't exist or is an admin.
    /// </summary>
    Task<AdminUserDetailResponse> GetUserByIdAsync(Guid id);

    /// <summary>
    /// Sets a user's status to Active or Pending.
    /// Throws ValidationException for invalid status strings.
    /// Throws ForbiddenException if the target user is an admin.
    /// Returns the updated user detail.
    /// </summary>
    Task<AdminUserDetailResponse> UpdateUserStatusAsync(Guid userId, string status, Guid adminId);
}
