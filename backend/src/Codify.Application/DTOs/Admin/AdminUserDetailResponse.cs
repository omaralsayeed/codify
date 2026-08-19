namespace Codify.Application.DTOs.Admin;

/// <summary>
/// Full user detail returned by GET /api/admin/users/:id.
/// Extends the list row with analytics fields and recent submissions.
/// </summary>
public class AdminUserDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>First letter of first name + first letter of last name, uppercase.</summary>
    public string Initials { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Numeric role: 0 = Student, 1 = Instructor.</summary>
    public int Role { get; set; }

    /// <summary>"active" or "pending".</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime RegisteredAt { get; set; }

    /// <summary>Last login timestamp. Null if never logged in after registration.</summary>
    public DateTime? LastActiveAt { get; set; }

    /// <summary>Institution name. Null for students.</summary>
    public string? Organization { get; set; }

    /// <summary>Count of problems with at least one accepted submission. Null for instructors.</summary>
    public int? ProblemsSolved { get; set; }

    /// <summary>Average score (0–100) across all submissions. Null for instructors.</summary>
    public decimal? AvgScore { get; set; }

    /// <summary>Current daily streak in days. Null for instructors.</summary>
    public int? Streak { get; set; }

    /// <summary>Total submission count. 0 for instructors.</summary>
    public int TotalSubmissions { get; set; }

    /// <summary>Last 5 submissions, newest first. Empty array for instructors.</summary>
    public List<AdminUserSubmissionRow> RecentSubmissions { get; set; } = [];
}
