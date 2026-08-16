using Codify.Application.Interfaces;
using Codify.Domain.Entities;
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

    public async Task<IReadOnlyList<User>> GetAllStudentsWithSubmissionsAsync() =>
        await db.Users
            .Where(u => u.Role == Domain.Enums.UserRole.Student && !u.IsDeleted)
            .Include(u => u.Submissions)
            .Include(u => u.PerformanceProfile)
            .AsSplitQuery()
            .OrderBy(u => u.FullName)
            .ToListAsync();

    public async Task<User?> GetUserWithProfileDataAsync(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return null;

        var clean = identifier.Trim().ToLower();

        if (Guid.TryParse(identifier, out var id))
        {
            return await db.Users
                .Include(u => u.Submissions)
                    .ThenInclude(s => s.Problem)
                        .ThenInclude(p => p.ProblemTags)
                            .ThenInclude(pt => pt.ConceptTag)
                .Include(u => u.PerformanceProfile)
                .AsSplitQuery()
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        return await db.Users
            .Include(u => u.Submissions)
                .ThenInclude(s => s.Problem)
                    .ThenInclude(p => p.ProblemTags)
                        .ThenInclude(pt => pt.ConceptTag)
            .Include(u => u.PerformanceProfile)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => !u.IsDeleted && (
                u.Username == identifier ||
                u.Email.ToLower() == clean ||
                u.FullName.ToLower() == clean ||
                u.FullName.ToLower().Replace(" ", "_") == clean
            ));
    }

    public async Task<IReadOnlyList<User>> GetAllUsersAsync() =>
        await db.Users
            .Where(u => !u.IsDeleted)
            .Include(u => u.Submissions)
            .AsSplitQuery()
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<User>> GetPendingInstructorsAsync() =>
        await db.Users
            .Where(u => u.Role == Domain.Enums.UserRole.Instructor
                     && u.Status == Domain.Enums.UserStatus.Pending)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(User user) =>
        await db.Users.AddAsync(user);

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
