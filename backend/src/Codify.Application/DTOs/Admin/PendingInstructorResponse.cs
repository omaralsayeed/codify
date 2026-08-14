namespace Codify.Application.DTOs.Admin;

/// <summary>
/// Represents an instructor whose account is pending admin approval.
/// </summary>
public class PendingInstructorResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public DateTime RegisteredAt { get; set; }
}
