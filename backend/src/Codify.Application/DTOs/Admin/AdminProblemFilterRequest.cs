namespace Codify.Application.DTOs.Admin;

/// <summary>
/// Query parameters for GET /api/admin/problems.
/// Returns ALL problems (including inactive), unlike GET /api/problems.
/// </summary>
public class AdminProblemFilterRequest
{
    /// <summary>Case-insensitive contains match on title.</summary>
    public string? Search { get; set; }

    /// <summary>0 = Easy, 1 = Medium, 2 = Hard. Omit to return all.</summary>
    public int? Difficulty { get; set; }

    /// <summary>Filter by tag name. Omit to return all.</summary>
    public string? Tag { get; set; }

    /// <summary>true = active only, false = inactive only. Omit to return all.</summary>
    public bool? IsActive { get; set; }

    /// <summary>"title" | "difficulty" | "solvedCount" | "createdAt" — default: createdAt</summary>
    public string SortBy { get; set; } = "createdAt";

    /// <summary>"asc" | "desc" — default: desc</summary>
    public string SortDir { get; set; } = "desc";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
