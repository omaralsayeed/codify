using Codify.Application.DTOs.Admin;
using Codify.Application.Interfaces;
using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class AdminService(
    IUserRepository userRepo,
    ISubmissionRepository submissionRepo,
    IProblemRepository problemRepo) : IAdminService
{
    // ── Legacy instructor approval (kept for existing endpoints) ──────────────

    public async Task<IReadOnlyList<PendingInstructorResponse>> GetPendingInstructorsAsync()
    {
        var instructors = await userRepo.GetPendingInstructorsAsync();

        return instructors.Select(u => new PendingInstructorResponse
        {
            UserId       = u.Id,
            FullName     = u.FullName,
            Email        = u.Email,
            Organization = u.Organization,
            RegisteredAt = u.CreatedAt
        }).ToList();
    }

    public async Task<ApproveInstructorResponse> ApproveInstructorAsync(Guid instructorId, Guid adminId)
    {
        var instructor = await userRepo.GetByIdAsync(instructorId)
            ?? throw new NotFoundException("Instructor not found.");

        if (instructor.Role != UserRole.Instructor)
            throw new ValidationException("The specified user is not an instructor.");

        if (instructor.Status == UserStatus.Active)
            throw new ValidationException("This instructor account is already active.");

        instructor.Approve(adminId);
        await userRepo.SaveChangesAsync();

        return new ApproveInstructorResponse
        {
            UserId     = instructor.Id,
            FullName   = instructor.FullName,
            Email      = instructor.Email,
            ApprovedAt = instructor.ReviewedAt!.Value
        };
    }

    // ── Admin panel endpoints ─────────────────────────────────────────────────

    public async Task<AdminStatsResponse> GetStatsAsync()
    {
        var todayMidnight = DateTime.UtcNow.Date;
        var weekAgo       = todayMidnight.AddDays(-6); // last 7 days inclusive

        // Fetch all non-admin users in one call then aggregate in memory —
        // avoids multiple DB round-trips for small platforms.
        var (users, _) = await userRepo.GetAdminUsersAsync(new AdminUserFilterRequest
        {
            Page     = 1,
            PageSize = int.MaxValue
        });

        int totalStudents      = users.Count(u => u.Role == UserRole.Student);
        int totalInstructors   = users.Count(u => u.Role == UserRole.Instructor);
        int activeInstructors  = users.Count(u => u.Role == UserRole.Instructor && u.Status == UserStatus.Active);
        int pendingInstructors = users.Count(u => u.Role == UserRole.Instructor && u.Status == UserStatus.Pending);

        int newUsersToday    = await userRepo.GetNewUsersCountAsync(todayMidnight);
        int newUsersThisWeek = await userRepo.GetNewUsersCountAsync(weekAgo);
        int totalProblems    = await problemRepo.GetTotalCountAsync();
        int submissionsToday = await submissionRepo.GetCountFromAsync(todayMidnight);
        int totalSubmissions = await submissionRepo.GetCountFromAsync(DateTime.MinValue);

        return new AdminStatsResponse
        {
            TotalUsers         = totalStudents + totalInstructors,
            TotalStudents      = totalStudents,
            TotalInstructors   = totalInstructors,
            ActiveInstructors  = activeInstructors,
            PendingInstructors = pendingInstructors,
            TotalProblems      = totalProblems,
            TotalSubmissions   = totalSubmissions,
            NewUsersToday      = newUsersToday,
            NewUsersThisWeek   = newUsersThisWeek,
            SubmissionsToday   = submissionsToday
        };
    }

    public async Task<(IReadOnlyList<AdminUserRow> Users, int Total)> GetUsersAsync(
        AdminUserFilterRequest filter)
    {
        var (items, total) = await userRepo.GetAdminUsersAsync(filter);
        return (items.Select(MapToUserRow).ToList(), total);
    }

    public async Task<AdminUserDetailResponse> GetUserByIdAsync(Guid id)
    {
        var user = await userRepo.GetByIdWithRecentSubmissionsAsync(id)
            ?? throw new NotFoundException("User not found.");

        return MapToUserDetail(user);
    }

    public async Task<AdminUserDetailResponse> UpdateUserStatusAsync(
        Guid userId, string status, Guid adminId)
    {
        var normalised = status.ToLower();
        if (normalised is not ("active" or "pending"))
            throw new ValidationException("Status must be 'active' or 'pending'.");

        var user = await userRepo.GetByIdWithRecentSubmissionsAsync(userId)
            ?? throw new NotFoundException("User not found.");

        if (user.Role == UserRole.Admin)
            throw new ForbiddenException("Cannot change the status of an admin account.");

        var newStatus = normalised == "active" ? UserStatus.Active : UserStatus.Pending;
        user.SetStatus(newStatus, adminId);
        await userRepo.SaveChangesAsync();

        return MapToUserDetail(user);
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    private static AdminUserRow MapToUserRow(User u) => new()
    {
        Id             = u.Id,
        Name           = u.FullName,
        Initials       = ComputeInitials(u.FullName),
        Email          = u.Email,
        Role           = (int)u.Role,
        Status         = u.Status.ToString().ToLower(),
        RegisteredAt   = u.CreatedAt,
        LastActiveAt   = u.LastLoginAt,
        ProblemsSolved = u.Role == UserRole.Student ? u.SolvedProblems : null,
        Organization   = u.Role == UserRole.Instructor ? u.Organization : null
    };

    private static AdminUserDetailResponse MapToUserDetail(User u)
    {
        var recentSubs = u.Submissions
            .OrderByDescending(s => s.SubmittedAt)
            .Take(5)
            .ToList();

        // Average score across all scored submissions
        decimal? avgScore = null;
        if (u.Role == UserRole.Student && u.Submissions.Any())
        {
            var scored = u.Submissions
                .Where(s => s.Score.HasValue)
                .Select(s => s.Score!.Value)
                .ToList();
            if (scored.Count > 0)
                avgScore = Math.Round(scored.Average(), 1);
        }

        // Streak: consecutive days with at least one accepted submission going back from today
        int? streak = null;
        if (u.Role == UserRole.Student)
        {
            var acceptedDays = u.Submissions
                .Where(s => s.Status == SubmissionStatus.Accepted)
                .Select(s => s.SubmittedAt.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            streak = ComputeStreak(acceptedDays);
        }

        return new AdminUserDetailResponse
        {
            Id               = u.Id,
            Name             = u.FullName,
            Initials         = ComputeInitials(u.FullName),
            Email            = u.Email,
            Role             = (int)u.Role,
            Status           = u.Status.ToString().ToLower(),
            RegisteredAt     = u.CreatedAt,
            LastActiveAt     = u.LastLoginAt,
            Organization     = u.Role == UserRole.Instructor ? u.Organization : null,
            ProblemsSolved   = u.Role == UserRole.Student ? u.SolvedProblems : null,
            AvgScore         = u.Role == UserRole.Student ? avgScore : null,
            Streak           = streak,
            TotalSubmissions = u.Submissions.Count,
            RecentSubmissions = recentSubs.Select(s => new AdminUserSubmissionRow
            {
                ProblemTitle = s.Problem?.Title ?? string.Empty,
                Status       = s.Status.ToString(),
                SubmittedAt  = s.SubmittedAt
            }).ToList()
        };
    }

    /// <summary>
    /// Derives initials from a full name: first character of each of the first two words.
    /// e.g. "Karim Ahmed" → "KA", "Dr. Hana Saad" → "DH"
    /// </summary>
    private static string ComputeInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        return string.Concat(
            fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(2)
                    .Select(w => char.ToUpper(w[0])));
    }

    /// <summary>
    /// Counts consecutive days (going backward from today) on which the student
    /// had at least one accepted submission. Input list must be sorted descending.
    /// </summary>
    private static int ComputeStreak(List<DateTime> sortedDescDays)
    {
        if (sortedDescDays.Count == 0) return 0;

        var today  = DateTime.UtcNow.Date;
        var cursor = today;
        int streak = 0;

        foreach (var day in sortedDescDays)
        {
            if (streak == 0 && (day == today || day == today.AddDays(-1)))
            {
                cursor = day;
                streak = 1;
            }
            else if (streak > 0 && day == cursor.AddDays(-1))
            {
                cursor = day;
                streak++;
            }
            else if (streak > 0)
            {
                break; // gap — streak ends here
            }
        }

        return streak;
    }
}
