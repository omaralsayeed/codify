using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Admin;

public class AdminStatsResponse
{
    public int TotalUsers { get; set; }
    public int ActiveStudents { get; set; }
    public int PendingInstructors { get; set; }
    public int ActiveInstructors { get; set; }
    public int TotalProblems { get; set; }
    public int TotalSubmissions { get; set; }
    public double PassRatePercent { get; set; }
    public int TotalContests { get; set; }
    public int AiFlagsCount { get; set; }
}

public class AdminUserListItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public int SolvedProblems { get; set; }
    public int TotalSubmissions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminUserDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string? Bio { get; set; }
    public decimal Rating { get; set; }
    public int SolvedProblems { get; set; }
    public int TotalSubmissions { get; set; }
    public double SuccessRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<AdminUserSubmissionDto> RecentSubmissions { get; set; } = [];
}

public class AdminUserSubmissionDto
{
    public Guid Id { get; set; }
    public Guid ProblemId { get; set; }
    public string ProblemTitle { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? ExecutionTimeMs { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class UpdateUserStatusRequest
{
    public UserStatus Status { get; set; }
}
