using Codify.Domain.Enums;

namespace Codify.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // ER diagram additions
    public string? Username { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public decimal Rating { get; private set; }
    public int SolvedProblems { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation
    public PerformanceProfile? PerformanceProfile { get; private set; }
    public ICollection<Submission> Submissions { get; private set; } = [];
    public ICollection<HintLog> HintLogs { get; private set; } = [];
    public ICollection<Problem> AuthoredProblems { get; private set; } = [];

    private User() { }

    public static User Create(string fullName, string email, string passwordHash, UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            Rating = 0,
            SolvedProblems = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    public void UpdateProfile(string? bio, string? avatarUrl)
    {
        Bio = bio;
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementSolvedProblems()
    {
        SolvedProblems++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
