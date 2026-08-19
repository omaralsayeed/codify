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

    /// <summary>Returns all active students with their submissions and performance profiles.</summary>
    Task<IReadOnlyList<User>> GetAllStudentsWithSubmissionsAsync();

    /// <summary>Returns students taught by/enrolled with the given instructor.</summary>
    Task<IReadOnlyList<User>> GetStudentsForInstructorAsync(Guid instructorId);

    /// <summary>Finds students by their email addresses.</summary>
    Task<IReadOnlyList<User>> GetStudentsByEmailsAsync(IEnumerable<string> emails);

    /// <summary>Checks if a student is taught by/enrolled with the given instructor.</summary>
    Task<bool> IsStudentEnrolledWithInstructorAsync(Guid instructorId, Guid studentId);

    /// <summary>Ensures an InstructorStudent relationship is recorded.</summary>
    Task EnsureInstructorStudentEnrolledAsync(Guid instructorId, Guid studentId);

    /// <summary>Finds a user by ID, username, email, or fullname slug with all submissions and problems.</summary>
    Task<User?> GetUserWithProfileDataAsync(string identifier);

    /// <summary>Returns all users across all roles.</summary>
    Task<IReadOnlyList<User>> GetAllUsersAsync();

    /// <summary>Returns all instructors with Status = Pending, ordered by registration date.</summary>
    Task<IReadOnlyList<User>> GetPendingInstructorsAsync();

    // ── Admin queries ─────────────────────────────────────────────────────────

    /// <summary>
    /// Paginated, filterable list of all non-admin users for the admin panel.
    /// Supports search by name/email, filter by role/status, sort, and paging.
    /// </summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetAdminUsersAsync(AdminUserFilterRequest filter);

    /// <summary>
    /// Returns a single non-admin user with their last 5 submissions (+ Problem title),
    /// PerformanceProfile, and the real total submission count.
    /// Returns null user if not found or if the user is an admin.
    /// </summary>
    Task<(User? User, int TotalSubmissions)> GetByIdWithRecentSubmissionsAsync(Guid id);

    /// <summary>Count of users registered on or after <paramref name="from"/> (UTC).</summary>
    Task<int> GetNewUsersCountAsync(DateTime from);

    /// <summary>Returns the last <paramref name="count"/> submissions for a user with Problem loaded.</summary>
    Task<IReadOnlyList<Submission>> GetRecentSubmissionsAsync(Guid userId, int count);

    /// <summary>Total submission count for a user.</summary>
    Task<int> GetTotalSubmissionsCountAsync(Guid userId);

    Task AddAsync(User user);
    Task SaveChangesAsync();
}
