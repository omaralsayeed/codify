using Codify.Application.DTOs.Admin;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Repositories;

public class UserRepository(CodifyDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id) =>
        await db.Users.FindAsync(id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await db.Users.FirstOrDefaultAsync(u => u.Email == email);

    /// <summary>
    /// Single split-query: User + Submissions (with Problem) + PerformanceProfile.
    /// AsSplitQuery avoids the cartesian product that would occur when including
    /// multiple collections in one SQL JOIN.
    /// </summary>
    public async Task<User?> GetWithAnalyticsDataAsync(Guid userId) =>
        await db.Users
            .Include(u => u.Submissions)
                .ThenInclude(s => s.Problem)
            .Include(u => u.PerformanceProfile)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == userId);

    /// <summary>
    /// Loads the instructor + all problems they authored.
    /// Each problem carries its submissions + the submitting user identity.
    ///
    /// Why load User inside Submission?
    ///   The analytics layer needs student name/email to build StudentSummaryItem.
    ///   We fetch it here in one DB round-trip rather than N lazy-load calls.
    /// </summary>
    public async Task<User?> GetInstructorWithProblemsAndSubmissionsAsync(Guid instructorId) =>
        await db.Users
            .Include(u => u.AuthoredProblems)
                .ThenInclude(p => p.Submissions)
                    .ThenInclude(s => s.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == instructorId);

    public async Task<IReadOnlyList<User>> GetPendingInstructorsAsync() =>
        await db.Users
            .Where(u => u.Role == UserRole.Instructor
                     && u.Status == UserStatus.Pending)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

    // ── Admin queries ─────────────────────────────────────────────────────────

    /// <summary>
    /// Paginated, filterable list of all non-admin users.
    /// Supports search by name/email, filter by role/status, sort, and paging.
    /// Admins are always excluded from this list.
    /// </summary>
    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetAdminUsersAsync(
        AdminUserFilterRequest filter)
    {
        var query = db.Users
            .Where(u => u.Role != UserRole.Admin && !u.IsDeleted)
            .AsQueryable();

        // Search: case-insensitive contains on name OR email
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term));
        }

        // Role filter
        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            var role = filter.Role.ToLower() switch
            {
                "student"    => UserRole.Student,
                "instructor" => UserRole.Instructor,
                _            => (UserRole?)null
            };
            if (role.HasValue)
                query = query.Where(u => u.Role == role.Value);
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var status = filter.Status.ToLower() switch
            {
                "active"  => UserStatus.Active,
                "pending" => UserStatus.Pending,
                _         => (UserStatus?)null
            };
            if (status.HasValue)
                query = query.Where(u => u.Status == status.Value);
        }

        // Sort
        query = (filter.SortBy?.ToLower(), filter.SortDir?.ToLower()) switch
        {
            ("name",         "asc")  => query.OrderBy(u => u.FullName),
            ("name",         _)      => query.OrderByDescending(u => u.FullName),
            ("lastactiveat", "asc")  => query.OrderBy(u => u.LastLoginAt),
            ("lastactiveat", _)      => query.OrderByDescending(u => u.LastLoginAt),
            ("registeredat", "asc")  => query.OrderBy(u => u.CreatedAt),
            _                        => query.OrderByDescending(u => u.CreatedAt)
        };

        var total = await query.CountAsync();

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <summary>
    /// Returns a single non-admin user with their last 5 submissions (+ Problem title),
    /// PerformanceProfile, and the real total submission count.
    ///
    /// Uses two separate queries because EF Core does not support filtered Include
    /// (.Take()) combined with AsSplitQuery on the same entity.
    /// </summary>
    public async Task<(User? User, int TotalSubmissions)> GetByIdWithRecentSubmissionsAsync(Guid id)
    {
        // Query 1: user + PerformanceProfile
        var user = await db.Users
            .Include(u => u.PerformanceProfile)
            .FirstOrDefaultAsync(u => u.Id == id && u.Role != UserRole.Admin);

        if (user is null) return (null, 0);

        // Query 2: last 5 submissions with Problem title
        var recentSubmissions = await db.Submissions
            .Include(s => s.Problem)
            .Where(s => s.UserId == id)
            .OrderByDescending(s => s.SubmittedAt)
            .Take(5)
            .ToListAsync();

        // Query 3: real total count (cheap COUNT(*))
        var totalCount = await db.Submissions.CountAsync(s => s.UserId == id);

        // Populate navigation collection so mapping layer can read them uniformly
        foreach (var sub in recentSubmissions)
            user.Submissions.Add(sub);

        return (user, totalCount);
    }

    /// <summary>Count of non-admin, non-deleted users registered on or after <paramref name="from"/> (UTC).</summary>
    public async Task<int> GetNewUsersCountAsync(DateTime from) =>
        await db.Users
            .CountAsync(u => u.Role != UserRole.Admin
                          && !u.IsDeleted
                          && u.CreatedAt >= from);

    /// <summary>Returns the last <paramref name="count"/> submissions for a user with Problem loaded.</summary>
    public async Task<IReadOnlyList<Submission>> GetRecentSubmissionsAsync(Guid userId, int count) =>
        await db.Submissions
            .Include(s => s.Problem)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SubmittedAt)
            .Take(count)
            .ToListAsync();

    /// <summary>Total submission count for a user (cheap COUNT query).</summary>
    public async Task<int> GetTotalSubmissionsCountAsync(Guid userId) =>
        await db.Submissions.CountAsync(s => s.UserId == userId);

    public async Task AddAsync(User user) =>
        await db.Users.AddAsync(user);

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
