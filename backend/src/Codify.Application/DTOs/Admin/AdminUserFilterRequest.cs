namespace Codify.Application.DTOs.Admin;

/// <summary>
/// Query parameters for GET /api/admin/users.
/// All filters are optional — omitting them returns everything.
/// </summary>
public class AdminUserFilterRequest
{
    /// <summary>Case-insensitive contains match on name OR email.</summary>
    public string? Search { get; set; }

    /// <summary>"student" | "instructor" — omit to return both.</summary>
    public string? Role { get; set; }

    /// <summary>"active" | "pending" — omit to return both.</summary>
    public string? Status { get; set; }

    /// <summary>"name" | "registeredAt" | "lastActiveAt" — default: registeredAt</summary>
    public string SortBy { get; set; } = "registeredAt";

    /// <summary>"asc" | "desc" — default: desc</summary>
    public string SortDir { get; set; } = "desc";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
