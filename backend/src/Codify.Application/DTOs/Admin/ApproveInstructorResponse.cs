namespace Codify.Application.DTOs.Admin;

public class ApproveInstructorResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; }
}
