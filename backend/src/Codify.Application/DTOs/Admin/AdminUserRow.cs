namespace Codify.Application.DTOs.Admin;

/// <summary>
/// A single row in the paginated user list returned by GET /api/admin/users.
/// </summary>
public class AdminUserRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>First letter of first name + first letter of last name, uppercase. e.g. "Karim Ahmed" → "KA".</summary>
    public string Initials { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Numeric role: 0 = Student, 1 = Instructor. Admins are never returned.</summary>
    public int Role { get; set; }

    /// <summary>"active" or "pending".</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime RegisteredAt { get; set; }

    /// <summary>Last login timestamp. Null if the user has never logged in after registration.</summary>
    public DateTime? LastActiveAt { get; set; }

    /// <summary>Count of accepted submissions. Null for instructors.</summary>
    public int? ProblemsSolved { get; set; }

    /// <summary>Institution name. Null for students.</summary>
    public string? Organization { get; set; }
}
