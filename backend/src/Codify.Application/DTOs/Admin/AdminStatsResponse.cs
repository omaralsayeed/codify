namespace Codify.Application.DTOs.Admin;

/// <summary>
/// Platform-wide statistics returned by GET /api/admin/stats.
/// Powers the admin overview dashboard.
/// </summary>
public class AdminStatsResponse
{
    /// <summary>Total non-admin users (students + instructors).</summary>
    public int TotalUsers { get; set; }

    public int TotalStudents { get; set; }
    public int TotalInstructors { get; set; }

    /// <summary>Instructors whose status is Active.</summary>
    public int ActiveInstructors { get; set; }

    /// <summary>Instructors whose status is Pending (awaiting approval).</summary>
    public int PendingInstructors { get; set; }

    public int TotalProblems { get; set; }
    public int TotalSubmissions { get; set; }

    /// <summary>Users who registered today (UTC midnight boundary).</summary>
    public int NewUsersToday { get; set; }

    /// <summary>Users who registered in the last 7 days.</summary>
    public int NewUsersThisWeek { get; set; }

    /// <summary>Submissions created today (UTC midnight boundary).</summary>
    public int SubmissionsToday { get; set; }
}
