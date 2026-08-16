using Codify.Application.DTOs.Admin;
using Codify.Domain.Entities;

namespace Codify.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Loads the user + their submissions (with Problem) + PerformanceProfile.
    /// Used by the student analytics query.
    /// </summary>
    Task<User?> GetWithAnalyticsDataAsync(Guid userId);

    /// <summary>
    /// Loads the instructor + their authored problems.
    /// Each problem includes all submissions (with the submitting User).
    /// Used by the instructor analytics query.
    /// </summary>
    Task<User?> GetInstructorWithProblemsAndSubmissionsAsync(Guid instructorId);

    /// <summary>Returns all instructors with Status = Pending, ordered by registration date.</summary>
    Task<IReadOnlyList<User>> GetPendingInstructorsAsync();

    // ── Admin queries ─────────────────────────────────────────────────────────

    /// <summary>
    /// Paginated, filterable list of all non-admin users for the admin panel.
    /// Supports search by name/email, filter by role/status, sort, and paging.
    /// </summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetAdminUsersAsync(AdminUserFilterRequest filter);

    /// <summary>
    /// Returns a single non-admin user with their last 5 submissions (+ Problem) and PerformanceProfile.
    /// Returns null if the user does not exist or is an admin.
    /// </summary>
    Task<User?> GetByIdWithRecentSubmissionsAsync(Guid id);

    /// <summary>Count of users registered on or after <paramref name="from"/> (UTC).</summary>
    Task<int> GetNewUsersCountAsync(DateTime from);

    Task AddAsync(User user);
    Task SaveChangesAsync();
}
