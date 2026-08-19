using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Auth;

public class UserProfileResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public string? Organization { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public int SolvedProblems { get; set; }
    public decimal Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}
