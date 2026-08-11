namespace Codify.Application.DTOs.Analytics;

public class StudentAnalyticsResponse
{
    // ── Identity ──────────────────────────────────────────────────
    public Guid   UserId   { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;

    // ── Solved problems ───────────────────────────────────────────
    public int TotalSolvedProblems { get; set; }
    public int EasySolved          { get; set; }
    public int MediumSolved        { get; set; }
    public int HardSolved          { get; set; }

    // ── Submission stats ──────────────────────────────────────────
    public int   TotalSubmissions    { get; set; }
    public int   AcceptedSubmissions { get; set; }
    public int   WrongAnswers        { get; set; }
    public int   RuntimeErrors       { get; set; }
    public int   CompileErrors       { get; set; }
    public int   TimeLimitExceeded   { get; set; }

    /// <summary>Accepted / Total * 100. Zero when no submissions.</summary>
    public double SuccessRatePercent { get; set; }

    // ── Performance ───────────────────────────────────────────────
    /// <summary>Average execution time across Accepted submissions (ms).</summary>
    public double? AverageExecutionTimeMs { get; set; }

    /// <summary>Average attempts before solving each problem.</summary>
    public double AverageAttemptsPerProblem { get; set; }

    // ── Language breakdown ────────────────────────────────────────
    /// <summary>How many submissions were made per language.</summary>
    public List<LanguageStatItem> LanguageBreakdown { get; set; } = [];

    // ── Concept strengths (from PerformanceProfile) ───────────────
    public List<string> StrongTopics { get; set; } = [];
    public List<string> WeakTopics   { get; set; } = [];

    // ── Activity ──────────────────────────────────────────────────
    public DateTime? LastSubmissionAt { get; set; }
    public DateTime  MemberSince      { get; set; }

    /// <summary>Total AI hints the student has requested across all problems.</summary>
    public int TotalHintsUsed { get; set; }
}

public class LanguageStatItem
{
    public string Language    { get; set; } = string.Empty;
    public int    Submissions { get; set; }
}
