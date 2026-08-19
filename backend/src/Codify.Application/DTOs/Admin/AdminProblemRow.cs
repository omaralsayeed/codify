namespace Codify.Application.DTOs.Admin;

/// <summary>
/// A single row in the admin problems list returned by GET /api/admin/problems.
/// Includes inactive problems — unlike the student-facing list.
/// </summary>
public class AdminProblemRow
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>0 = Easy, 1 = Medium, 2 = Hard.</summary>
    public int Difficulty { get; set; }

    public List<string> Tags { get; set; } = [];

    /// <summary>Count of distinct users who have at least one Accepted submission.</summary>
    public int SolvedCount { get; set; }

    /// <summary>Total submission attempts against this problem.</summary>
    public int TotalSubmissions { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
