namespace Codify.Application.DTOs.Analytics;

public class InstructorAnalyticsResponse
{
    // ── Instructor identity ───────────────────────────────────────
    public Guid   InstructorId   { get; set; }
    public string FullName       { get; set; } = string.Empty;
    public string Email          { get; set; } = string.Empty;

    // ── Overview ──────────────────────────────────────────────────
    /// <summary>Total problems this instructor has authored.</summary>
    public int TotalProblemsAuthored { get; set; }

    /// <summary>Distinct students who submitted on at least one of their problems.</summary>
    public int TotalStudentsReached { get; set; }

    /// <summary>Total submissions across all authored problems.</summary>
    public int TotalSubmissionsReceived { get; set; }

    /// <summary>Accept rate across all submissions on authored problems.</summary>
    public double OverallAcceptRatePercent { get; set; }

    // ── Per-student summary ───────────────────────────────────────
    public List<StudentSummaryItem> Students { get; set; } = [];
}

/// <summary>
/// Lightweight summary per student — not the full StudentAnalyticsResponse.
/// Instructors see a dashboard view; they can drill into a student
/// via GET /api/analytics/students/{id} for the full detail.
/// </summary>
public class StudentSummaryItem
{
    public Guid   StudentId          { get; set; }
    public string FullName           { get; set; } = string.Empty;
    public string Email              { get; set; } = string.Empty;

    /// <summary>Submissions on this instructor's problems only.</summary>
    public int    TotalSubmissions   { get; set; }
    public int    AcceptedSubmissions { get; set; }
    public double SuccessRatePercent  { get; set; }

    /// <summary>Distinct problems solved (Accepted) from this instructor's set.</summary>
    public int    ProblemsSolved      { get; set; }

    public DateTime? LastActivityAt   { get; set; }
}
